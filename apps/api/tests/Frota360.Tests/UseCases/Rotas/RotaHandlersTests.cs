using Frota360.Application.Common;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Rotas.Commands.CreateRota;
using Frota360.Application.UseCases.Rotas.Commands.DeleteRota;
using Frota360.Application.UseCases.Rotas.Commands.EncerrarRota;
using Frota360.Application.UseCases.Rotas.Commands.UpdateRota;
using Frota360.Application.UseCases.Rotas.Queries.GetAllRotas;
using Frota360.Application.UseCases.Rotas.Queries.GetMinhasRotas;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Rotas
{
    public class RotaHandlersTests
    {
        private readonly IRotaRepository _repository = Substitute.For<IRotaRepository>();
        private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
        private readonly IVeiculoRepository _veiculoRepository = Substitute.For<IVeiculoRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();

        public RotaHandlersTests()
        {
            _currentUser.EmpresaId.Returns(1);
        }

        private CreateRotaHandler CriarCreateHandler() =>
            new(_repository, _usuarioRepository, _veiculoRepository, _currentUser, _auditoria, NullLogger<CreateRotaHandler>.Instance);

        private UpdateRotaHandler CriarUpdateHandler() =>
            new(_repository, _usuarioRepository, _veiculoRepository, _currentUser, _auditoria, NullLogger<UpdateRotaHandler>.Instance);

        private EncerrarRotaHandler CriarEncerrarHandler() =>
            new(_repository, _veiculoRepository, _currentUser, _auditoria, NullLogger<EncerrarRotaHandler>.Instance);

        private static Rota NovaRota(int id = 1,
                                     int kmInicial = 50_000,
                                     DateTime? dataFim = null,
                                     int codigoVeiculo = 1,
                                     int codigoMotorista = 1) => new()
        {
            Id = id,
            EmpresaId = 1,
            Origem = "Curitiba",
            Destino = "São Paulo",
            CodigoMotorista = codigoMotorista,
            CodigoVeiculo = codigoVeiculo,
            Ativo = dataFim is null,
            DataInicio = new DateTime(2024, 1, 1),
            DataFim = dataFim,
            KmInicial = kmInicial,
            DataInclusao = new DateTime(2024, 1, 1),
            // O repositório carrega por Include; aqui o fake faz o mesmo.
            Motorista = NovoMotorista(codigoMotorista)
        };

        /// <summary>O motorista é um usuário com a role Motorista — não há entidade própria.</summary>
        private static Usuario NovoMotorista(int id = 1) => new()
        {
            Id = id,
            EmpresaId = 1,
            Nome = "João da Silva",
            Email = "joao@empresa.com",
            Role = Roles.Motorista,
            Ativo = true,
            DataInclusao = new DateTime(2024, 1, 1)
        };

        private static Veiculo NovoVeiculo(int id = 1, int quilometragem = 50_000) => new()
        {
            Id = id,
            EmpresaId = 1,
            NomeVeiculo = "Sprinter",
            MarcaVeiculo = "Mercedes-Benz",
            Placa = "ABC1D23",
            Quilometragem = quilometragem,
            DataInclusao = new DateTime(2024, 1, 1)
        };

        private static CreateRotaRequest NovaCreateRequest(int kmInicial = 50_000) => new()
        {
            Origem = "Joinville",
            Destino = "Blumenau",
            CodigoMotorista = 2,
            CodigoVeiculo = 3,
            DataInicio = new DateTime(2025, 6, 1),
            KmInicial = kmInicial
        };

        private static UpdateRotaRequest NovaUpdateRequest() => new()
        {
            Origem = "Curitiba",
            Destino = "Florianópolis",
            CodigoMotorista = 1,
            CodigoVeiculo = 1,
            DataInicio = new DateTime(2024, 1, 1)
        };

        // ----------------------------------------------------------------- Create

        [Fact]
        public async Task Create_DevePersistirEMapearResposta()
        {
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns(NovoMotorista(2));
            _veiculoRepository.GetByIdAsync(3, 1).Returns(NovoVeiculo(3));
            _repository.AddAsync(Arg.Any<Rota>())
                .Returns(ci =>
                {
                    var r = ci.Arg<Rota>();
                    r.Id = 8;
                    return r;
                });

            var handler = CriarCreateHandler();

            var resposta = await handler.HandleAsync(new CreateRotaCommand(NovaCreateRequest()));

            Assert.Equal(8, resposta.Id);
            Assert.Equal("Joinville", resposta.Origem);
            Assert.Equal("Blumenau", resposta.Destino);
            Assert.Equal(50_000, resposta.KmInicial);
            Assert.Null(resposta.KmFinal);
            Assert.Null(resposta.KmPercorrido);
            await _repository.Received(1).AddAsync(Arg.Is<Rota>(
                r => r.EmpresaId == 1 && r.Ativo && r.DataFim == null && r.DataInclusao != default));
        }

        [Fact]
        public async Task Create_DeveResolverFksEscopadasNaEmpresa()
        {
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns(NovoMotorista(2));
            _veiculoRepository.GetByIdAsync(3, 1).Returns(NovoVeiculo(3));
            _repository.AddAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());

            var handler = CriarCreateHandler();

            await handler.HandleAsync(new CreateRotaCommand(NovaCreateRequest()));

            await _usuarioRepository.Received(1).GetMotoristaByIdAsync(2, 1);
            await _veiculoRepository.Received(1).GetByIdAsync(3, 1);
            await _repository.Received(1).AddAsync(Arg.Is<Rota>(r => r.CodigoMotorista == 2 && r.CodigoVeiculo == 3));
        }

        [Fact]
        public async Task Create_QuandoMotoristaNaoExiste_DeveLancarInvalidOperationException()
        {
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns((Usuario?)null);
            _veiculoRepository.GetByIdAsync(3, 1).Returns(NovoVeiculo(3));

            var handler = CriarCreateHandler();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateRotaCommand(NovaCreateRequest())));

            Assert.Equal("Motorista 2 não encontrado.", ex.Message);
        }

        [Fact]
        public async Task Create_QuandoVeiculoNaoExiste_DeveLancarInvalidOperationException()
        {
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns(NovoMotorista(2));
            _veiculoRepository.GetByIdAsync(3, 1).Returns((Veiculo?)null);

            var handler = CriarCreateHandler();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateRotaCommand(NovaCreateRequest())));

            Assert.Equal("Veículo 3 não encontrado.", ex.Message);
        }

        [Fact]
        public async Task Create_QuandoMotoristaNaoExiste_NaoDevePersistir()
        {
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns((Usuario?)null);

            var handler = CriarCreateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateRotaCommand(NovaCreateRequest())));

            await _repository.DidNotReceive().AddAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Create_QuandoKmInicialAbaixoDoOdometro_DeveRecusar()
        {
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns(NovoMotorista(2));
            _veiculoRepository.GetByIdAsync(3, 1).Returns(NovoVeiculo(3, quilometragem: 61_000));

            var handler = CriarCreateHandler();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateRotaCommand(NovaCreateRequest(kmInicial: 60_000))));

            Assert.Equal("A quilometragem inicial não pode ser menor que o odômetro atual do veículo (61000 km).", ex.Message);
            await _repository.DidNotReceive().AddAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Create_QuandoKmInicialAcimaDoOdometro_DeveAvancarOdometro()
        {
            var veiculo = NovoVeiculo(3, quilometragem: 50_000);
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns(NovoMotorista(2));
            _veiculoRepository.GetByIdAsync(3, 1).Returns(veiculo);
            _repository.AddAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());

            var handler = CriarCreateHandler();

            await handler.HandleAsync(new CreateRotaCommand(NovaCreateRequest(kmInicial: 52_400)));

            Assert.Equal(52_400, veiculo.Quilometragem);
            await _veiculoRepository.Received(1).UpdateAsync(veiculo);
        }

        [Fact]
        public async Task Create_QuandoKmInicialIgualAoOdometro_NaoDeveTocarNoVeiculo()
        {
            var veiculo = NovoVeiculo(3, quilometragem: 50_000);
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns(NovoMotorista(2));
            _veiculoRepository.GetByIdAsync(3, 1).Returns(veiculo);
            _repository.AddAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());

            var handler = CriarCreateHandler();

            await handler.HandleAsync(new CreateRotaCommand(NovaCreateRequest(kmInicial: 50_000)));

            Assert.Equal(50_000, veiculo.Quilometragem);
            await _veiculoRepository.DidNotReceive().UpdateAsync(Arg.Any<Veiculo>());
        }

        // ----------------------------------------------------------------- Update

        [Fact]
        public async Task Update_QuandoExiste_DeveAtualizarERetornarResposta()
        {
            _repository.GetByIdAsync(7, 1).Returns(NovaRota(7));
            _usuarioRepository.GetMotoristaByIdAsync(1, 1).Returns(NovoMotorista(1));
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo(1));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());

            var handler = CriarUpdateHandler();

            var resposta = await handler.HandleAsync(new UpdateRotaCommand(7, NovaUpdateRequest()));

            Assert.NotNull(resposta);
            Assert.Equal("Florianópolis", resposta!.Destino);
            await _repository.Received(1).UpdateAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Update_NaoDeveMexerNoEstadoDeEncerramento()
        {
            var rota = NovaRota(7, kmInicial: 50_000);
            _repository.GetByIdAsync(7, 1).Returns(rota);
            _usuarioRepository.GetMotoristaByIdAsync(1, 1).Returns(NovoMotorista(1));
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo(1));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());

            var handler = CriarUpdateHandler();

            var resposta = await handler.HandleAsync(new UpdateRotaCommand(7, NovaUpdateRequest()));

            // Encerrar é a única transição de estado: o PUT não desativa nem fecha a rota.
            Assert.True(resposta!.Ativo);
            Assert.Null(resposta.DataFim);
            Assert.Null(resposta.KmFinal);
            Assert.Equal(50_000, resposta.KmInicial);
        }

        [Fact]
        public async Task Update_DeveResolverFksEscopadasNaEmpresa()
        {
            _repository.GetByIdAsync(7, 1).Returns(NovaRota(7));
            _usuarioRepository.GetMotoristaByIdAsync(1, 1).Returns(NovoMotorista(1));
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo(1));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());

            var handler = CriarUpdateHandler();

            await handler.HandleAsync(new UpdateRotaCommand(7, NovaUpdateRequest()));

            await _repository.Received(1).GetByIdAsync(7, 1);
            await _usuarioRepository.Received(1).GetMotoristaByIdAsync(1, 1);
            await _veiculoRepository.Received(1).GetByIdAsync(1, 1);
        }

        [Fact]
        public async Task Update_QuandoMotoristaNaoExiste_DeveLancarInvalidOperationException()
        {
            _repository.GetByIdAsync(7, 1).Returns(NovaRota(7));
            _usuarioRepository.GetMotoristaByIdAsync(1, 1).Returns((Usuario?)null);
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo(1));

            var handler = CriarUpdateHandler();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new UpdateRotaCommand(7, NovaUpdateRequest())));

            Assert.Equal("Motorista 1 não encontrado.", ex.Message);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Update_QuandoVeiculoNaoExiste_DeveLancarInvalidOperationException()
        {
            _repository.GetByIdAsync(7, 1).Returns(NovaRota(7));
            _usuarioRepository.GetMotoristaByIdAsync(1, 1).Returns(NovoMotorista(1));
            _veiculoRepository.GetByIdAsync(1, 1).Returns((Veiculo?)null);

            var handler = CriarUpdateHandler();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new UpdateRotaCommand(7, NovaUpdateRequest())));

            Assert.Equal("Veículo 1 não encontrado.", ex.Message);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Update_QuandoNaoExiste_DeveRetornarNull()
        {
            _repository.GetByIdAsync(99, 1).Returns((Rota?)null);

            var handler = CriarUpdateHandler();

            var resposta = await handler.HandleAsync(
                new UpdateRotaCommand(99, new UpdateRotaRequest()));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Rota>());
            // A rota inexistente sai antes de qualquer resolução de FK.
            await _usuarioRepository.DidNotReceive().GetMotoristaByIdAsync(Arg.Any<int>(), Arg.Any<int>());
            await _veiculoRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<int>());
        }

        // --------------------------------------------------------------- Encerrar

        [Fact]
        public async Task Encerrar_DeveCalcularKmPercorridoEAvancarOdometro()
        {
            var veiculo = NovoVeiculo(1, quilometragem: 50_000);
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(veiculo);

            var request = new EncerrarRotaRequest
            {
                KmFinal = 50_430,
                DataFim = new DateTime(2024, 1, 2)
            };

            var resposta = await CriarEncerrarHandler().HandleAsync(new EncerrarRotaCommand(5, request));

            Assert.NotNull(resposta);
            Assert.False(resposta!.Ativo);
            Assert.Equal(50_430, resposta.KmFinal);
            Assert.Equal(430, resposta.KmPercorrido);
            Assert.Equal(new DateTime(2024, 1, 2), resposta.DataFim);
            Assert.Equal(50_430, veiculo.Quilometragem);
            await _veiculoRepository.Received(1).UpdateAsync(veiculo);
        }

        [Fact]
        public async Task Encerrar_QuandoDataFimOmitida_DeveUsarAgora()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo(1));

            var antes = DateTime.Now;

            var resposta = await CriarEncerrarHandler().HandleAsync(
                new EncerrarRotaCommand(5, new EncerrarRotaRequest { KmFinal = 50_100 }));

            Assert.NotNull(resposta!.DataFim);
            Assert.InRange(resposta.DataFim!.Value, antes, DateTime.Now);
        }

        [Fact]
        public async Task Encerrar_QuandoKmFinalMenorQueInicial_DeveRecusar()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000));

            var request = new EncerrarRotaRequest { KmFinal = 49_900 };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarEncerrarHandler().HandleAsync(new EncerrarRotaCommand(5, request)));

            Assert.Equal("A quilometragem final não pode ser menor que a inicial.", ex.Message);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Encerrar_QuandoRotaJaEncerrada_DeveRecusar()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, dataFim: new DateTime(2024, 1, 5)));

            var request = new EncerrarRotaRequest { KmFinal = 60_000 };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarEncerrarHandler().HandleAsync(new EncerrarRotaCommand(5, request)));

            Assert.Equal("Esta rota já foi encerrada.", ex.Message);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Encerrar_QuandoDataFimAnteriorADataInicio_DeveRecusar()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000));

            var request = new EncerrarRotaRequest
            {
                KmFinal = 50_400,
                DataFim = new DateTime(2023, 12, 31) // rota começa em 2024-01-01
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarEncerrarHandler().HandleAsync(new EncerrarRotaCommand(5, request)));

            Assert.Equal("A data de fim não pode ser anterior à data de início.", ex.Message);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Encerrar_QuandoKmFinalMenorQueOdometroAtual_NaoDeveRetroagirOdometro()
        {
            var veiculo = NovoVeiculo(1, quilometragem: 70_000);
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(veiculo);

            var request = new EncerrarRotaRequest { KmFinal = 50_430, DataFim = new DateTime(2024, 1, 2) };

            var resposta = await CriarEncerrarHandler().HandleAsync(new EncerrarRotaCommand(5, request));

            // A rota registra o próprio percurso, mas o odômetro do veículo não retrocede.
            Assert.Equal(430, resposta!.KmPercorrido);
            Assert.Equal(70_000, veiculo.Quilometragem);
            // O veículo ainda é salvo: o encerramento também grava a ficha da última
            // viagem, que independe de o odômetro ter avançado ou não.
            Assert.Equal("João da Silva", veiculo.UltimoMotorista);
        }

        [Fact]
        public async Task Encerrar_QuandoNaoExiste_DeveRetornarNull()
        {
            _repository.GetByIdAsync(99, 1).Returns((Rota?)null);

            var resposta = await CriarEncerrarHandler().HandleAsync(
                new EncerrarRotaCommand(99, new EncerrarRotaRequest()));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Rota>());
            await _veiculoRepository.DidNotReceive().UpdateAsync(Arg.Any<Veiculo>());
        }

        [Fact]
        public async Task Encerrar_DeveBuscarRotaEVeiculoEscopadosNaEmpresa()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000, codigoVeiculo: 4));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());
            _veiculoRepository.GetByIdAsync(4, 1).Returns(NovoVeiculo(4));

            await CriarEncerrarHandler().HandleAsync(
                new EncerrarRotaCommand(5, new EncerrarRotaRequest { KmFinal = 50_500 }));

            await _repository.Received(1).GetByIdAsync(5, 1);
            await _veiculoRepository.Received(1).GetByIdAsync(4, 1);
        }

        // ------------------------------------------------------------ Delete/Get

        [Fact]
        public async Task Delete_QuandoExiste_DeveRemoverERetornarTrue()
        {
            var existente = NovaRota(6);
            _repository.GetByIdAsync(6, 1).Returns(existente);

            var handler = new DeleteRotaHandler(_repository, _currentUser, _auditoria, NullLogger<DeleteRotaHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteRotaCommand(6));

            Assert.True(resultado);
            await _repository.Received(1).DeleteAsync(existente);
        }

        [Fact]
        public async Task Delete_QuandoNaoExiste_DeveRetornarFalse()
        {
            _repository.GetByIdAsync(123, 1).Returns((Rota?)null);

            var handler = new DeleteRotaHandler(_repository, _currentUser, _auditoria, NullLogger<DeleteRotaHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteRotaCommand(123));

            Assert.False(resultado);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task GetAll_DeveMapearTodasAsRotas()
        {
            _repository.GetAllAsync(1).Returns(new[] { NovaRota(1), NovaRota(2), NovaRota(3) });

            var handler = new GetAllRotasHandler(_repository, _currentUser, NullLogger<GetAllRotasHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllRotasQuery())).ToList();

            Assert.Equal(3, resposta.Count);
        }

        // ------------------------------------------------- Nome e ficha do veículo

        [Fact]
        public async Task Create_DeveDesnormalizarONomeDoMotorista()
        {
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns(NovoMotorista(2));
            _veiculoRepository.GetByIdAsync(3, 1).Returns(NovoVeiculo(3));
            _repository.AddAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());

            var resposta = await CriarCreateHandler().HandleAsync(new CreateRotaCommand(NovaCreateRequest()));

            Assert.Equal("João da Silva", resposta.NomeMotorista);
        }

        [Fact]
        public async Task GetAll_DeveDesnormalizarONomeDoMotorista()
        {
            // É o que mantém a rota identificável depois que a pessoa é rebaixada e some
            // da lista de motoristas.
            _repository.GetAllAsync(1).Returns(new[] { NovaRota(1) });

            var handler = new GetAllRotasHandler(_repository, _currentUser, NullLogger<GetAllRotasHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllRotasQuery())).Single();

            Assert.Equal("João da Silva", resposta.NomeMotorista);
        }

        [Fact]
        public async Task Encerrar_DeveRegistrarUltimoMotoristaEDataNoVeiculo()
        {
            var veiculo = NovoVeiculo(1, quilometragem: 50_000);
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(veiculo);

            await CriarEncerrarHandler().HandleAsync(new EncerrarRotaCommand(5,
                new EncerrarRotaRequest { KmFinal = 50_430, DataFim = new DateTime(2024, 1, 2) }));

            Assert.Equal("João da Silva", veiculo.UltimoMotorista);
            Assert.Equal(new DateTime(2024, 1, 2), veiculo.DataUltimaViagem);
            await _veiculoRepository.Received(1).UpdateAsync(veiculo);
        }

        [Fact]
        public async Task Encerrar_ComViagemMaisAntigaQueARegistrada_NaoDeveRetroagirAFichaDoVeiculo()
        {
            // Mesma política do odômetro: encerrar hoje uma rota de mês passado não pode
            // reescrever o veículo com dado velho.
            var veiculo = NovoVeiculo(1, quilometragem: 50_000);
            veiculo.UltimoMotorista = "Maria";
            veiculo.DataUltimaViagem = new DateTime(2024, 6, 1);
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(veiculo);

            await CriarEncerrarHandler().HandleAsync(new EncerrarRotaCommand(5,
                new EncerrarRotaRequest { KmFinal = 50_430, DataFim = new DateTime(2024, 1, 2) }));

            Assert.Equal("Maria", veiculo.UltimoMotorista);
            Assert.Equal(new DateTime(2024, 6, 1), veiculo.DataUltimaViagem);
            // O odômetro ainda avança: ele não depende da data, e 50.430 > 50.000.
            Assert.Equal(50_430, veiculo.Quilometragem);
        }

        // -------------------------------------------------------------- Motorista
        //
        // Segundo eixo de isolamento: além da empresa, a role Motorista só alcança as
        // rotas do próprio login — e o id vem sempre do token, nunca do cliente.

        /// <summary>Coloca a requisição no papel do motorista com o id informado.</summary>
        private void ComoMotorista(int usuarioId)
        {
            _currentUser.Role.Returns(Roles.Motorista);
            _currentUser.UsuarioId.Returns(usuarioId);
        }

        private GetMinhasRotasHandler CriarMinhasRotasHandler() =>
            new(_repository, _currentUser, NullLogger<GetMinhasRotasHandler>.Instance);

        [Fact]
        public async Task GetMinhasRotas_DeveConsultarEscopadoNaEmpresaENoMotoristaDaClaim()
        {
            ComoMotorista(2);
            _repository.GetAllByMotoristaAsync(1, 2).Returns(new[] { NovaRota(1, codigoMotorista: 2), NovaRota(2, codigoMotorista: 2) });

            var resposta = (await CriarMinhasRotasHandler().HandleAsync(new GetMinhasRotasQuery())).ToList();

            Assert.Equal(2, resposta.Count);
            await _repository.Received(1).GetAllByMotoristaAsync(1, 2);
            await _repository.DidNotReceive().GetAllAsync(Arg.Any<int>());
        }


        [Fact]
        public async Task Create_ComoMotorista_DeveIgnorarOCodigoMotoristaDoCorpo()
        {
            // O corpo pede a rota no nome do motorista 2; a claim diz 9. Vence a claim.
            ComoMotorista(9);
            _usuarioRepository.GetMotoristaByIdAsync(9, 1).Returns(NovoMotorista(9));
            _veiculoRepository.GetByIdAsync(3, 1).Returns(NovoVeiculo(3));
            _repository.AddAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());

            await CriarCreateHandler().HandleAsync(new CreateRotaCommand(NovaCreateRequest()));

            await _usuarioRepository.Received(1).GetMotoristaByIdAsync(9, 1);
            await _usuarioRepository.DidNotReceive().GetMotoristaByIdAsync(2, Arg.Any<int>());
            await _repository.Received(1).AddAsync(Arg.Is<Rota>(r => r.CodigoMotorista == 9));
        }


        [Fact]
        public async Task Create_ComUsuarioQueNaoEhMotorista_DeveRecusar()
        {
            // O repositório filtra por role: um Supervisor "não existe" como motorista.
            _usuarioRepository.GetMotoristaByIdAsync(2, 1).Returns((Usuario?)null);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarCreateHandler().HandleAsync(new CreateRotaCommand(NovaCreateRequest())));

            Assert.Equal("Motorista 2 não encontrado.", ex.Message);
            await _repository.DidNotReceive().AddAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Encerrar_ComoMotorista_DeveEncerrarAPropriaRota()
        {
            ComoMotorista(9);
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000, codigoMotorista: 9));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo(1));

            var resposta = await CriarEncerrarHandler().HandleAsync(
                new EncerrarRotaCommand(5, new EncerrarRotaRequest { KmFinal = 50_400 }));

            Assert.NotNull(resposta);
            Assert.Equal(400, resposta!.KmPercorrido);
        }

        [Fact]
        public async Task Encerrar_ComoMotorista_RotaDeOutro_DeveRetornarNull()
        {
            // 404 e não 403: para quem não é dono dela, a rota não existe.
            ComoMotorista(9);
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000, codigoMotorista: 4));

            var resposta = await CriarEncerrarHandler().HandleAsync(
                new EncerrarRotaCommand(5, new EncerrarRotaRequest { KmFinal = 50_400 }));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Rota>());
            await _veiculoRepository.DidNotReceive().UpdateAsync(Arg.Any<Veiculo>());
        }

        /// <summary>
        /// O salto de odômetro é justamente o que hoje ninguém consegue rastrear: o
        /// encerramento avança a quilometragem do veículo, e o diff precisa mostrar isso.
        /// </summary>
        [Fact]
        public async Task Encerrar_DeveRegistrarAuditoriaComOAvancoDoOdometro()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5, kmInicial: 50_000));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo(1));

            await CriarEncerrarHandler().HandleAsync(
                new EncerrarRotaCommand(5, new EncerrarRotaRequest { KmFinal = 50_400 }));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Rota,
                AcoesAuditoria.Encerrou,
                5,
                Arg.Any<string>(),
                Arg.Is<IEnumerable<AlteracaoCampo>>(a => a.Any(c => c.Campo.StartsWith("Odômetro do veículo"))));
        }

        [Fact]
        public async Task Delete_DeveRegistrarAuditoria()
        {
            _repository.GetByIdAsync(5, 1).Returns(NovaRota(5));

            var handler = new DeleteRotaHandler(_repository, _currentUser, _auditoria, NullLogger<DeleteRotaHandler>.Instance);

            await handler.HandleAsync(new DeleteRotaCommand(5));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Rota, AcoesAuditoria.Excluiu, 5, Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>>());
        }
    }
}
