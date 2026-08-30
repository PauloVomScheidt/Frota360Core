using Frota360.Application.Common;
using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Veiculos.Commands.CreateVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.DeleteVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.UpdateVeiculo;
using Frota360.Application.UseCases.Veiculos.Queries.GetAllVeiculos;
using Frota360.Application.UseCases.Veiculos.Queries.GetVeiculoById;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Veiculos
{
    public class VeiculoHandlersTests
    {
        private readonly IVeiculoRepository _repository = Substitute.For<IVeiculoRepository>();
        private readonly IRotaRepository _rotaRepository = Substitute.For<IRotaRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();
        private readonly IAbastecimentoRepository _abastecimentoRepository = Substitute.For<IAbastecimentoRepository>();

        private DeleteVeiculoHandler CriarDeleteHandler() =>
            new(_repository, _rotaRepository, _abastecimentoRepository, _currentUser, _auditoria, NullLogger<DeleteVeiculoHandler>.Instance);

        public VeiculoHandlersTests()
        {
            _currentUser.EmpresaId.Returns(1);
        }

        private static Veiculo NovoVeiculo(int id = 1) => new()
        {
            Id = id,
            NomeVeiculo = "Strada",
            MarcaVeiculo = "Fiat",
            Placa = "ABC1D23",
            Quilometragem = 50_000,
            DataInclusao = new DateTime(2024, 1, 1)
        };

        [Fact]
        public async Task Create_DevePersistirEMapearResposta()
        {
            _repository.AddAsync(Arg.Any<Veiculo>())
                .Returns(ci =>
                {
                    var v = ci.Arg<Veiculo>();
                    v.Id = 3;
                    return v;
                });

            var handler = new CreateVeiculoHandler(_repository, _currentUser, _auditoria, NullLogger<CreateVeiculoHandler>.Instance);
            var request = new CreateVeiculoRequest
            {
                NomeVeiculo = "Saveiro",
                MarcaVeiculo = "VW",
                Placa = "XYZ9K88",
                Quilometragem = 1200
            };

            var resposta = await handler.HandleAsync(new CreateVeiculoCommand(request));

            Assert.Equal(3, resposta.Id);
            Assert.Equal("Saveiro", resposta.NomeVeiculo);
            Assert.Equal("XYZ9K88", resposta.Placa);
            await _repository.Received(1).AddAsync(Arg.Is<Veiculo>(v => v.EmpresaId == 1 && v.DataInclusao != default));
        }

        [Fact]
        public async Task Update_QuandoExiste_DeveAtualizarERetornarResposta()
        {
            _repository.GetByIdAsync(7, 1).Returns(NovoVeiculo(7));
            _repository.UpdateAsync(Arg.Any<Veiculo>()).Returns(ci => ci.Arg<Veiculo>());

            var handler = new UpdateVeiculoHandler(_repository, _rotaRepository, _currentUser, _auditoria, NullLogger<UpdateVeiculoHandler>.Instance);
            var request = new UpdateVeiculoRequest
            {
                NomeVeiculo = "Strada Volcano",
                MarcaVeiculo = "Fiat",
                Placa = "ABC1D23",
                Quilometragem = 60_000
            };

            var resposta = await handler.HandleAsync(new UpdateVeiculoCommand(7, request));

            Assert.NotNull(resposta);
            Assert.Equal("Strada Volcano", resposta!.NomeVeiculo);
            Assert.Equal(60_000, resposta.Quilometragem);
            await _repository.Received(1).UpdateAsync(Arg.Any<Veiculo>());
        }

        [Fact]
        public async Task Update_QuandoNaoExiste_DeveRetornarNull()
        {
            _repository.GetByIdAsync(99, 1).Returns((Veiculo?)null);

            var handler = new UpdateVeiculoHandler(_repository, _rotaRepository, _currentUser, _auditoria, NullLogger<UpdateVeiculoHandler>.Instance);

            var resposta = await handler.HandleAsync(
                new UpdateVeiculoCommand(99, new UpdateVeiculoRequest()));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Veiculo>());
        }

        /// <summary>RN09 — a placa é persistida sempre em maiúsculas, venha como vier.</summary>
        [Fact]
        public async Task Create_DeveNormalizarAPlacaParaMaiusculas()
        {
            _repository.AddAsync(Arg.Any<Veiculo>()).Returns(ci => ci.Arg<Veiculo>());

            var handler = new CreateVeiculoHandler(_repository, _currentUser, _auditoria, NullLogger<CreateVeiculoHandler>.Instance);

            var resposta = await handler.HandleAsync(new CreateVeiculoCommand(new CreateVeiculoRequest
            {
                NomeVeiculo = "Saveiro",
                MarcaVeiculo = "VW",
                Placa = " abc1d23 ",
                Quilometragem = 0
            }));

            Assert.Equal("ABC1D23", resposta.Placa);
        }

        [Fact]
        public async Task Delete_QuandoExiste_DeveRemoverERetornarTrue()
        {
            var existente = NovoVeiculo(4);
            _repository.GetByIdAsync(4, 1).Returns(existente);
            _rotaRepository.ExisteComVeiculoAsync(1, 4).Returns(false);

            var handler = CriarDeleteHandler();

            var resultado = await handler.HandleAsync(new DeleteVeiculoCommand(4));

            Assert.True(resultado);
            await _repository.Received(1).DeleteAsync(existente);
        }

        /// <summary>
        /// RN08 — o veículo com rota associada não pode sumir: a rota guarda o histórico de
        /// quilometragem e ficaria apontando para um registro inexistente.
        /// </summary>
        [Fact]
        public async Task Delete_ComRotasAssociadas_DeveLancarENaoRemover()
        {
            _repository.GetByIdAsync(4, 1).Returns(NovoVeiculo(4));
            _rotaRepository.ExisteComVeiculoAsync(1, 4).Returns(true);

            var handler = CriarDeleteHandler();

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.HandleAsync(new DeleteVeiculoCommand(4)));

            Assert.Equal(
                "Não é possível excluir um veículo com rotas associadas. Encerre ou remova as rotas antes.",
                erro.Message);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Veiculo>());
        }

        [Fact]
        public async Task Delete_QuandoNaoExiste_DeveRetornarFalse()
        {
            _repository.GetByIdAsync(123, 1).Returns((Veiculo?)null);

            var handler = CriarDeleteHandler();

            var resultado = await handler.HandleAsync(new DeleteVeiculoCommand(123));

            Assert.False(resultado);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Veiculo>());
        }

        [Fact]
        public async Task GetAll_DeveMapearTodosOsVeiculos()
        {
            _repository.GetAllAsync(1).Returns(new[] { NovoVeiculo(1), NovoVeiculo(2) });
            _rotaRepository.GetVeiculosEmRotaAsync(1).Returns([]);

            var handler = new GetAllVeiculosHandler(_repository, _rotaRepository, _currentUser, NullLogger<GetAllVeiculosHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllVeiculosQuery())).ToList();

            Assert.Equal(2, resposta.Count);
        }

        /// <summary>
        /// `EmRota` é derivado na leitura, como `Atrasada` na manutenção — e a consulta é
        /// uma só para a lista inteira, não uma por veículo.
        /// </summary>
        [Fact]
        public async Task GetAll_DeveMarcarComoEmRotaSoOsVeiculosComRotaAberta()
        {
            _repository.GetAllAsync(1).Returns(new[] { NovoVeiculo(1), NovoVeiculo(2), NovoVeiculo(3) });
            _rotaRepository.GetVeiculosEmRotaAsync(1).Returns(new[] { 2 });

            var handler = new GetAllVeiculosHandler(_repository, _rotaRepository, _currentUser, NullLogger<GetAllVeiculosHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllVeiculosQuery())).ToList();

            Assert.False(resposta.Single(v => v.Id == 1).EmRota);
            Assert.True(resposta.Single(v => v.Id == 2).EmRota);
            Assert.False(resposta.Single(v => v.Id == 3).EmRota);

            // Escopado na empresa do token, e uma única consulta para os três veículos.
            await _rotaRepository.Received(1).GetVeiculosEmRotaAsync(1);
        }

        [Fact]
        public async Task GetById_DeveConsultarARotaAbertaDaquelVeiculoEscopadaNaEmpresa()
        {
            _repository.GetByIdAsync(7, 1).Returns(NovoVeiculo(7));
            _rotaRepository.ExisteRotaAtivaComVeiculoAsync(1, 7).Returns(true);

            var handler = new GetVeiculoByIdHandler(_repository, _rotaRepository, _currentUser,
                NullLogger<GetVeiculoByIdHandler>.Instance);

            var resposta = await handler.HandleAsync(new GetVeiculoByIdQuery(7));

            Assert.True(resposta!.EmRota);
            await _rotaRepository.Received(1).ExisteRotaAtivaComVeiculoAsync(1, 7);
        }

        /// <summary>Editar a ficha não tira o carro da estrada.</summary>
        [Fact]
        public async Task Update_DevePreservarEmRotaNaResposta()
        {
            _repository.GetByIdAsync(7, 1).Returns(NovoVeiculo(7));
            _repository.UpdateAsync(Arg.Any<Veiculo>()).Returns(ci => ci.Arg<Veiculo>());
            _rotaRepository.ExisteRotaAtivaComVeiculoAsync(1, 7).Returns(true);

            var handler = new UpdateVeiculoHandler(_repository, _rotaRepository, _currentUser, _auditoria, NullLogger<UpdateVeiculoHandler>.Instance);

            var resposta = await handler.HandleAsync(new UpdateVeiculoCommand(7, new UpdateVeiculoRequest
            {
                NomeVeiculo = "Strada Volcano",
                MarcaVeiculo = "Fiat",
                Placa = "ABC1D23",
                Quilometragem = 60_000
            }));

            Assert.True(resposta!.EmRota);
        }

        [Fact]
        public async Task Update_DeveRegistrarAuditoriaComODiffDosCamposAlterados()
        {
            _repository.GetByIdAsync(7, 1).Returns(NovoVeiculo(7));
            _repository.UpdateAsync(Arg.Any<Veiculo>()).Returns(ci => ci.Arg<Veiculo>());

            var handler = new UpdateVeiculoHandler(_repository, _rotaRepository, _currentUser, _auditoria, NullLogger<UpdateVeiculoHandler>.Instance);

            await handler.HandleAsync(new UpdateVeiculoCommand(7, new UpdateVeiculoRequest
            {
                NomeVeiculo = "Strada",
                MarcaVeiculo = "Fiat",
                Placa = "XYZ9K87",     // era ABC1D23
                Quilometragem = 50_000 // igual: não deve entrar no diff
            }));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Veiculo,
                AcoesAuditoria.Atualizou,
                7,
                Arg.Any<string>(),
                Arg.Is<IEnumerable<AlteracaoCampo>>(a =>
                    a.Count() == 1 && a.Single().Campo == "Placa" && a.Single().Para == "XYZ9K87"));
        }
    }
}
