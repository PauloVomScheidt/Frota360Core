using Frota360.Application.Common;
using Frota360.Application.DTOs.Manutencao.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Manutencoes.Commands.ConcluirManutencao;
using Frota360.Application.UseCases.Manutencoes.Commands.CreateManutencao;
using Frota360.Application.UseCases.Manutencoes.Commands.DeleteManutencao;
using Frota360.Application.UseCases.Manutencoes.Commands.UpdateManutencao;
using Frota360.Application.UseCases.Manutencoes.Queries.GetAllManutencoes;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Enums;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Manutencoes
{
    public class ManutencaoHandlersTests
    {
        private readonly IManutencaoRepository _repository = Substitute.For<IManutencaoRepository>();
        private readonly IVeiculoRepository _veiculoRepository = Substitute.For<IVeiculoRepository>();
        private readonly ITipoManutencaoRepository _tipoRepository = Substitute.For<ITipoManutencaoRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();

        public ManutencaoHandlersTests()
        {
            _currentUser.EmpresaId.Returns(1);
        }

        private static Veiculo NovoVeiculo(int id = 1, int quilometragem = 50_000) => new()
        {
            Id = id,
            EmpresaId = 1,
            NomeVeiculo = "Fit",
            MarcaVeiculo = "Honda",
            Placa = "ABC1D23",
            Quilometragem = quilometragem
        };

        private static TipoManutencao NovoTipo(int id = 1, bool ativo = true) => new()
        {
            Id = id,
            EmpresaId = 1,
            Nome = "Troca de óleo",
            IntervaloKm = 10_000,
            Ativo = ativo
        };

        private static Manutencao NovaManutencao(int id = 1,
                                                 StatusManutencao status = StatusManutencao.Pendente,
                                                 int quilometragemPrevista = 60_000,
                                                 int quilometragemVeiculo = 50_000) => new()
        {
            Id = id,
            EmpresaId = 1,
            VeiculoId = 1,
            TipoManutencaoId = 1,
            QuilometragemPrevista = quilometragemPrevista,
            Status = status,
            DataInclusao = new DateTime(2026, 1, 1),
            Veiculo = NovoVeiculo(quilometragem: quilometragemVeiculo),
            Tipo = NovoTipo()
        };

        private CreateManutencaoHandler CreateHandler() =>
            new(_repository, _veiculoRepository, _tipoRepository, _currentUser, _auditoria, NullLogger<CreateManutencaoHandler>.Instance);

        private UpdateManutencaoHandler UpdateHandler() =>
            new(_repository, _veiculoRepository, _tipoRepository, _currentUser, _auditoria, NullLogger<UpdateManutencaoHandler>.Instance);

        private ConcluirManutencaoHandler ConcluirHandler() =>
            new(_repository, _veiculoRepository, _currentUser, _auditoria, NullLogger<ConcluirManutencaoHandler>.Instance);

        [Fact]
        public async Task Create_DevePersistirEscopadoNaEmpresaEMapearResposta()
        {
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo());
            _tipoRepository.GetByIdAsync(1, 1).Returns(NovoTipo());
            _repository.AddAsync(Arg.Any<Manutencao>()).Returns(ci =>
            {
                var m = ci.Arg<Manutencao>();
                m.Id = 9;
                return m;
            });

            var request = new CreateManutencaoRequest
            {
                VeiculoId = 1,
                TipoManutencaoId = 1,
                QuilometragemPrevista = 60_000
            };

            var resposta = await CreateHandler().HandleAsync(new CreateManutencaoCommand(request));

            Assert.Equal(9, resposta.Id);
            Assert.Equal("Pendente", resposta.Status);
            Assert.Equal("Troca de óleo", resposta.TipoManutencaoNome);
            Assert.Equal("ABC1D23", resposta.VeiculoPlaca);
            Assert.Equal(10_000, resposta.KmRestantes);
            Assert.False(resposta.Atrasada);
            await _repository.Received(1).AddAsync(Arg.Is<Manutencao>(m => m.EmpresaId == 1 && m.DataInclusao != default));
        }

        [Fact]
        public async Task Create_QuandoVeiculoEDeOutraEmpresa_NaoDevePersistir()
        {
            // Repositório escopado por empresa não devolve o veículo alheio, mesmo com o id correto no corpo.
            _veiculoRepository.GetByIdAsync(42, 1).Returns((Veiculo?)null);
            _tipoRepository.GetByIdAsync(1, 1).Returns(NovoTipo());

            var request = new CreateManutencaoRequest { VeiculoId = 42, TipoManutencaoId = 1, QuilometragemPrevista = 60_000 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateHandler().HandleAsync(new CreateManutencaoCommand(request)));

            await _repository.DidNotReceive().AddAsync(Arg.Any<Manutencao>());
        }

        [Fact]
        public async Task Create_QuandoTipoEstaInativo_NaoDevePersistir()
        {
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo());
            _tipoRepository.GetByIdAsync(1, 1).Returns(NovoTipo(ativo: false));

            var request = new CreateManutencaoRequest { VeiculoId = 1, TipoManutencaoId = 1, QuilometragemPrevista = 60_000 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateHandler().HandleAsync(new CreateManutencaoCommand(request)));

            await _repository.DidNotReceive().AddAsync(Arg.Any<Manutencao>());
        }

        [Fact]
        public async Task Create_QuandoJaExisteAgendamentoIgualPendente_NaoDevePersistir()
        {
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo());
            _tipoRepository.GetByIdAsync(1, 1).Returns(NovoTipo());
            _repository.ExisteDuplicadaAsync(1, 1, 1, 60_000, null).Returns(true);

            var request = new CreateManutencaoRequest { VeiculoId = 1, TipoManutencaoId = 1, QuilometragemPrevista = 60_000 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateHandler().HandleAsync(new CreateManutencaoCommand(request)));

            await _repository.DidNotReceive().AddAsync(Arg.Any<Manutencao>());
        }

        [Fact]
        public async Task Update_QuandoNaoExiste_DeveRetornarNull()
        {
            _repository.GetByIdAsync(99, 1).Returns((Manutencao?)null);

            var resposta = await UpdateHandler().HandleAsync(
                new UpdateManutencaoCommand(99, new UpdateManutencaoRequest()));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Manutencao>());
        }

        [Fact]
        public async Task Update_QuandoJaRealizada_NaoDeveAlterarHistorico()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaManutencao(5, StatusManutencao.Realizada));

            var request = new UpdateManutencaoRequest { VeiculoId = 1, TipoManutencaoId = 1, QuilometragemPrevista = 70_000 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => UpdateHandler().HandleAsync(new UpdateManutencaoCommand(5, request)));

            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Manutencao>());
        }

        [Fact]
        public async Task Update_QuandoPendente_DeveReplanejar()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaManutencao(5));
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo());
            _tipoRepository.GetByIdAsync(1, 1).Returns(NovoTipo());
            _repository.UpdateAsync(Arg.Any<Manutencao>()).Returns(ci => ci.Arg<Manutencao>());

            var request = new UpdateManutencaoRequest { VeiculoId = 1, TipoManutencaoId = 1, QuilometragemPrevista = 70_000 };

            var resposta = await UpdateHandler().HandleAsync(new UpdateManutencaoCommand(5, request));

            Assert.NotNull(resposta);
            Assert.Equal(70_000, resposta!.QuilometragemPrevista);
            Assert.Equal(20_000, resposta.KmRestantes);
            await _repository.Received(1).UpdateAsync(Arg.Any<Manutencao>());
        }

        [Fact]
        public async Task Concluir_DeveRegistrarExecucaoEAvancarOdometroDoVeiculo()
        {
            var veiculo = NovoVeiculo(quilometragem: 59_000);
            _repository.GetByIdAsync(5, 1).Returns(NovaManutencao(5));
            _repository.UpdateAsync(Arg.Any<Manutencao>()).Returns(ci => ci.Arg<Manutencao>());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(veiculo);

            var request = new ConcluirManutencaoRequest
            {
                QuilometragemRealizada = 61_230,
                DataRealizacao = new DateTime(2026, 3, 12),
                Custo = 380m
            };

            var resposta = await ConcluirHandler().HandleAsync(new ConcluirManutencaoCommand(5, request));

            Assert.NotNull(resposta);
            Assert.Equal("Realizada", resposta!.Status);
            Assert.Equal(61_230, resposta.QuilometragemRealizada);
            Assert.Equal(380m, resposta.Custo);
            Assert.Null(resposta.KmRestantes);
            Assert.False(resposta.Atrasada);
            Assert.Equal(61_230, veiculo.Quilometragem);
            await _veiculoRepository.Received(1).UpdateAsync(veiculo);
        }

        [Fact]
        public async Task Concluir_QuandoKmInformadoEMenorQueOAtual_NaoDeveRetroagirOdometro()
        {
            var veiculo = NovoVeiculo(quilometragem: 70_000);
            _repository.GetByIdAsync(5, 1).Returns(NovaManutencao(5));
            _repository.UpdateAsync(Arg.Any<Manutencao>()).Returns(ci => ci.Arg<Manutencao>());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(veiculo);

            var request = new ConcluirManutencaoRequest
            {
                QuilometragemRealizada = 61_230,
                DataRealizacao = new DateTime(2026, 3, 12)
            };

            await ConcluirHandler().HandleAsync(new ConcluirManutencaoCommand(5, request));

            Assert.Equal(70_000, veiculo.Quilometragem);
            await _veiculoRepository.DidNotReceive().UpdateAsync(Arg.Any<Veiculo>());
        }

        [Fact]
        public async Task Concluir_QuandoJaConcluida_DeveRecusar()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaManutencao(5, StatusManutencao.Realizada));

            var request = new ConcluirManutencaoRequest { QuilometragemRealizada = 61_230, DataRealizacao = DateTime.UtcNow };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ConcluirHandler().HandleAsync(new ConcluirManutencaoCommand(5, request)));

            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Manutencao>());
        }

        [Fact]
        public async Task Concluir_QuandoNaoExiste_DeveRetornarNull()
        {
            _repository.GetByIdAsync(99, 1).Returns((Manutencao?)null);

            var resposta = await ConcluirHandler().HandleAsync(
                new ConcluirManutencaoCommand(99, new ConcluirManutencaoRequest()));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Manutencao>());
        }

        [Fact]
        public async Task GetAll_DeveRepassarFiltrosEMapear()
        {
            _repository.GetAllAsync(1, 1, StatusManutencao.Pendente)
                .Returns(new[] { NovaManutencao(1), NovaManutencao(2) });

            var handler = new GetAllManutencoesHandler(_repository, _currentUser, NullLogger<GetAllManutencoesHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllManutencoesQuery(1, StatusManutencao.Pendente))).ToList();

            Assert.Equal(2, resposta.Count);
            await _repository.Received(1).GetAllAsync(1, 1, StatusManutencao.Pendente);
        }

        [Fact]
        public async Task GetAll_DeveRepassarOPeriodoAoRepositorio()
        {
            var de = new DateTime(2026, 8, 1);
            var ate = new DateTime(2026, 8, 11);

            _repository.GetAllAsync(1, null, null, de, ate).Returns([]);

            var handler = new GetAllManutencoesHandler(_repository, _currentUser, NullLogger<GetAllManutencoesHandler>.Instance);

            await handler.HandleAsync(new GetAllManutencoesQuery(De: de, Ate: ate));

            await _repository.Received(1).GetAllAsync(1, null, null, de, ate);
        }

        /// <summary>
        /// Intervalo invertido devolveria lista vazia sem explicar o porquê — vira 422 com
        /// o texto que a tela mostra ao usuário.
        /// </summary>
        [Fact]
        public async Task GetAll_ComPeriodoInvertido_DeveLancar()
        {
            var handler = new GetAllManutencoesHandler(_repository, _currentUser, NullLogger<GetAllManutencoesHandler>.Instance);

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.HandleAsync(new GetAllManutencoesQuery(
                    De: new DateTime(2026, 8, 11), Ate: new DateTime(2026, 8, 1))));

            Assert.Equal("A data final do período não pode ser anterior à inicial.", erro.Message);
            await _repository.DidNotReceive().GetAllAsync(
                Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<StatusManutencao?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>());
        }

        [Fact]
        public async Task Delete_QuandoExiste_DeveRemoverERetornarTrue()
        {
            var existente = NovaManutencao(4);
            _repository.GetByIdAsync(4, 1).Returns(existente);

            var handler = new DeleteManutencaoHandler(_repository, _currentUser, _auditoria, NullLogger<DeleteManutencaoHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteManutencaoCommand(4));

            Assert.True(resultado);
            await _repository.Received(1).DeleteAsync(existente);
        }

        [Fact]
        public async Task Delete_QuandoNaoExiste_DeveRetornarFalse()
        {
            _repository.GetByIdAsync(123, 1).Returns((Manutencao?)null);

            var handler = new DeleteManutencaoHandler(_repository, _currentUser, _auditoria, NullLogger<DeleteManutencaoHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteManutencaoCommand(123));

            Assert.False(resultado);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Manutencao>());
        }

        [Fact]
        public async Task Concluir_DeveRegistrarAuditoriaComATransicaoDeStatus()
        {
            var pendente = NovaManutencao(9);
            _repository.GetByIdAsync(9, 1).Returns(pendente);
            _repository.UpdateAsync(Arg.Any<Manutencao>()).Returns(ci => ci.Arg<Manutencao>());
            _veiculoRepository.GetByIdAsync(pendente.VeiculoId, 1).Returns(NovoVeiculo(pendente.VeiculoId));

            var handler = new ConcluirManutencaoHandler(_repository, _veiculoRepository, _currentUser, _auditoria,
                NullLogger<ConcluirManutencaoHandler>.Instance);

            await handler.HandleAsync(new ConcluirManutencaoCommand(9, new ConcluirManutencaoRequest
            {
                QuilometragemRealizada = 61_240,
                DataRealizacao = new DateTime(2026, 8, 28)
            }));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Manutencao,
                AcoesAuditoria.Concluiu,
                9,
                Arg.Any<string>(),
                Arg.Is<IEnumerable<AlteracaoCampo>>(a =>
                    a.Any(c => c.Campo == "Status" && c.Para == nameof(StatusManutencao.Realizada))));
        }

        [Fact]
        public async Task Delete_QuandoNaoExiste_NaoDeveRegistrarAuditoria()
        {
            _repository.GetByIdAsync(123, 1).Returns((Manutencao?)null);

            var handler = new DeleteManutencaoHandler(_repository, _currentUser, _auditoria, NullLogger<DeleteManutencaoHandler>.Instance);

            await handler.HandleAsync(new DeleteManutencaoCommand(123));

            // Nada aconteceu de fato: um "não encontrado" não é evento de trilha.
            await _auditoria.DidNotReceive().RegistrarAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>>());
        }
    }
}
