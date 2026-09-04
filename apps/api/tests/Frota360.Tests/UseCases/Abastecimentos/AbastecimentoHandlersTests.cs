using Frota360.Application.Common;
using Frota360.Application.DTOs.Abastecimento.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Abastecimentos.Commands.CreateAbastecimento;
using Frota360.Application.UseCases.Abastecimentos.Commands.DeleteAbastecimento;
using Frota360.Application.UseCases.Abastecimentos.Commands.UpdateAbastecimento;
using Frota360.Application.UseCases.Abastecimentos.Queries.GetAbastecimentoById;
using Frota360.Application.UseCases.Abastecimentos.Queries.GetAllAbastecimentos;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Abastecimentos
{
    public class AbastecimentoHandlersTests
    {
        private readonly IAbastecimentoRepository _repository = Substitute.For<IAbastecimentoRepository>();
        private readonly IVeiculoRepository _veiculoRepository = Substitute.For<IVeiculoRepository>();
        private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
        private readonly IRotaRepository _rotaRepository = Substitute.For<IRotaRepository>();
        private readonly ITipoCombustivelRepository _tipoCombustivelRepository = Substitute.For<ITipoCombustivelRepository>();
        private readonly IPostoRepository _postoRepository = Substitute.For<IPostoRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();

        public AbastecimentoHandlersTests()
        {
            _currentUser.EmpresaId.Returns(1);
            _currentUser.UsuarioId.Returns(10);
            _currentUser.Role.Returns(Roles.Supervisor);

            // Sem rota aberta é o caso comum; os testes de rota sobrescrevem.
            _rotaRepository.GetAllByMotoristaAsync(Arg.Any<int>(), Arg.Any<int>()).Returns([]);
        }

        private void ComoMotorista(int usuarioId)
        {
            _currentUser.Role.Returns(Roles.Motorista);
            _currentUser.UsuarioId.Returns(usuarioId);
        }

        private static Veiculo NovoVeiculo(int id = 1, int quilometragem = 60_000) => new()
        {
            Id = id,
            EmpresaId = 1,
            NomeVeiculo = "Strada",
            MarcaVeiculo = "Fiat",
            Placa = "ABC1D23",
            Quilometragem = quilometragem
        };

        private static Usuario NovoMotorista(int id = 7, string nome = "João Lima") =>
            new() { Id = id, EmpresaId = 1, Nome = nome, Role = Roles.Motorista };

        private static Abastecimento NovoAbastecimento(int id = 1,
                                                       int motoristaId = 7,
                                                       int usuarioId = 10,
                                                       decimal valor = 320m) => new()
        {
            Id = id,
            EmpresaId = 1,
            VeiculoId = 1,
            MotoristaId = motoristaId,
            UsuarioId = usuarioId,
            TipoCombustivelId = 3,
            PostoId = 4,
            Litros = 50m,
            ValorLitro = 6.40m,
            Valor = valor,
            Odometro = 60_500,
            NotaFiscal = "123456",
            DataAbastecimento = new DateTime(2026, 8, 28),
            Veiculo = NovoVeiculo(),
            Motorista = NovoMotorista(motoristaId),
            Usuario = new Usuario { Id = usuarioId, Nome = "Ana Souza" },
            TipoCombustivel = NovoTipoCombustivel(),
            Posto = NovoPosto()
        };

        private static TipoCombustivel NovoTipoCombustivel(int id = 3, string nome = "Diesel S10", bool ativo = true) =>
            new() { Id = id, EmpresaId = 1, Nome = nome, Ativo = ativo };

        private static Posto NovoPosto(int id = 4, string nome = "Posto Ipiranga BR-101", bool ativo = true) =>
            new() { Id = id, EmpresaId = 1, Nome = nome, Ativo = ativo };

        private static Rota RotaAberta(int id = 42, int motoristaId = 7, int veiculoId = 1) => new()
        {
            Id = id,
            EmpresaId = 1,
            CodigoMotorista = motoristaId,
            CodigoVeiculo = veiculoId,
            Ativo = true,
            DataFim = null
        };

        private CreateAbastecimentoHandler CriarCreateHandler() =>
            new(_repository, _veiculoRepository, _usuarioRepository, _rotaRepository,
                _tipoCombustivelRepository, _postoRepository, _currentUser, _auditoria,
                NullLogger<CreateAbastecimentoHandler>.Instance);

        private UpdateAbastecimentoHandler CriarUpdateHandler() =>
            new(_repository, _veiculoRepository, _tipoCombustivelRepository, _postoRepository,
                _currentUser, _auditoria, NullLogger<UpdateAbastecimentoHandler>.Instance);

        /// <summary>Atalho do update: catálogos resolvidos e um corpo válido.</summary>
        private void PrepararUpdate()
        {
            _tipoCombustivelRepository.GetByIdAsync(3, 1).Returns(NovoTipoCombustivel());
            _postoRepository.GetByIdAsync(4, 1).Returns(NovoPosto());
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo());
        }

        private static UpdateAbastecimentoRequest CorpoDeCorrecao(decimal litros = 50m,
                                                                 decimal valorLitro = 6.40m,
                                                                 int odometro = 60_500) => new()
        {
            TipoCombustivelId = 3,
            PostoId = 4,
            Litros = litros,
            ValorLitro = valorLitro,
            Odometro = odometro,
            NotaFiscal = "123456",
            DataAbastecimento = new DateTime(2026, 8, 28)
        };

        private GetAllAbastecimentosHandler CriarGetAllHandler() =>
            new(_repository, _currentUser, NullLogger<GetAllAbastecimentosHandler>.Instance);

        private static CreateAbastecimentoRequest RequisicaoValida(int veiculoId = 1, int? motoristaId = 7) => new()
        {
            VeiculoId = veiculoId,
            MotoristaId = motoristaId,
            TipoCombustivelId = 3,
            PostoId = 4,
            Litros = 50m,
            ValorLitro = 6.40m,
            Odometro = 61_000,
            NotaFiscal = "123456",
            DataAbastecimento = new DateTime(2026, 8, 28)
        };

        /// <summary>Atalho para o caminho feliz do create: veículo, motorista e persistência já resolvidos.</summary>
        private void PrepararCreate(int abastecimentoId = 5, int motoristaId = 7)
        {
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo());
            _usuarioRepository.GetMotoristaByIdAsync(motoristaId, 1).Returns(NovoMotorista(motoristaId));
            _tipoCombustivelRepository.GetByIdAsync(3, 1).Returns(NovoTipoCombustivel());
            _postoRepository.GetByIdAsync(4, 1).Returns(NovoPosto());
            _repository.AddAsync(Arg.Any<Abastecimento>()).Returns(ci =>
            {
                var a = ci.Arg<Abastecimento>();
                a.Id = abastecimentoId;
                return a;
            });
            _repository.GetByIdAsync(abastecimentoId, 1).Returns(NovoAbastecimento(abastecimentoId, motoristaId));
        }

        // ---------- Create ----------

        [Fact]
        public async Task Create_ComoGestao_DevePersistirEscopadoNaEmpresaComMotoristaDoCorpoEUsuarioDoToken()
        {
            PrepararCreate();

            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida()));

            // O gasto é do motorista 7; o registro é do supervisor 10 que digitou.
            await _repository.Received(1).AddAsync(Arg.Is<Abastecimento>(a =>
                a.EmpresaId == 1 && a.VeiculoId == 1 && a.MotoristaId == 7 && a.UsuarioId == 10));
        }

        [Fact]
        public async Task Create_ComVeiculoDeOutraEmpresa_DeveLancar()
        {
            // O repositório escopado devolve null: para esta empresa o id não existe.
            _veiculoRepository.GetByIdAsync(99, 1).Returns((Veiculo?)null);

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida(veiculoId: 99))));

            Assert.Equal("Veículo 99 não encontrado.", erro.Message);
            await _repository.DidNotReceive().AddAsync(Arg.Any<Abastecimento>());
        }

        [Fact]
        public async Task Create_ComoGestao_SemMotorista_DeveLancar()
        {
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo());

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida(motoristaId: null))));

            Assert.Equal("Informe o motorista do abastecimento.", erro.Message);
            await _repository.DidNotReceive().AddAsync(Arg.Any<Abastecimento>());
        }

        /// <summary>
        /// GetMotoristaByIdAsync filtra empresa <b>e</b> role: usuário de outra empresa e
        /// usuário que não é motorista caem os dois no mesmo "não existe".
        /// </summary>
        [Fact]
        public async Task Create_ComMotoristaInvalido_DeveLancar()
        {
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo());
            _usuarioRepository.GetMotoristaByIdAsync(99, 1).Returns((Usuario?)null);

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida(motoristaId: 99))));

            Assert.Equal("Motorista 99 não encontrado.", erro.Message);
            await _repository.DidNotReceive().AddAsync(Arg.Any<Abastecimento>());
        }

        /// <summary>
        /// O motorista não escolhe de quem é o gasto: a API ignora o id do corpo e usa o
        /// usuário do token — como no CreateRotaHandler.
        /// </summary>
        [Fact]
        public async Task Create_ComoMotorista_DeveIgnorarOMotoristaDoCorpoEUsarOProprio()
        {
            ComoMotorista(7);
            PrepararCreate();

            // Tenta lançar na conta de outro: precisa ser ignorado.
            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida(motoristaId: 99)));

            await _repository.Received(1).AddAsync(Arg.Is<Abastecimento>(a => a.MotoristaId == 7 && a.UsuarioId == 7));
            await _usuarioRepository.DidNotReceive().GetMotoristaByIdAsync(99, 1);
        }

        [Fact]
        public async Task Create_ComoMotorista_ComRotaAbertaNoMesmoVeiculo_DeveVincularARota()
        {
            ComoMotorista(7);
            PrepararCreate();
            _rotaRepository.GetAllByMotoristaAsync(1, 7).Returns(new[] { RotaAberta(veiculoId: 1) });

            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida()));

            await _repository.Received(1).AddAsync(Arg.Is<Abastecimento>(a => a.RotaId == 42));
        }

        /// <summary>
        /// Trava de veículo: quem está em rota abastece o carro da rota. É a mesma regra que
        /// o front aplica no select — aqui ela é a autoridade.
        /// </summary>
        [Fact]
        public async Task Create_ComoMotorista_ComRotaAbertaEmOutroVeiculo_DeveLancar()
        {
            ComoMotorista(7);
            _veiculoRepository.GetByIdAsync(1, 1).Returns(NovoVeiculo());
            _usuarioRepository.GetMotoristaByIdAsync(7, 1).Returns(NovoMotorista());
            _rotaRepository.GetAllByMotoristaAsync(1, 7).Returns(new[] { RotaAberta(veiculoId: 2) });

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida())));

            Assert.Equal("Você está em rota com outro veículo. Lance o abastecimento no veículo da sua rota aberta.",
                erro.Message);
            await _repository.DidNotReceive().AddAsync(Arg.Any<Abastecimento>());
        }

        [Fact]
        public async Task Create_ComoMotorista_SemRotaAberta_DeveAceitarQualquerVeiculoESemRota()
        {
            ComoMotorista(7);
            PrepararCreate();
            // Rota já encerrada: não trava nem vincula.
            _rotaRepository.GetAllByMotoristaAsync(1, 7).Returns(new[]
            {
                new Rota { Id = 40, EmpresaId = 1, CodigoMotorista = 7, CodigoVeiculo = 2, Ativo = false, DataFim = new DateTime(2026, 8, 20) }
            });

            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida()));

            await _repository.Received(1).AddAsync(Arg.Is<Abastecimento>(a => a.RotaId == null));
        }

        /// <summary>
        /// A gestão não é travada pela rota — troca de carro e apoio existem —, mas o
        /// vínculo é feito quando o veículo bate com o da rota aberta do motorista.
        /// </summary>
        [Fact]
        public async Task Create_ComoGestao_ComRotaAbertaDoMotoristaNaqueleVeiculo_DeveVincularARota()
        {
            PrepararCreate();
            _rotaRepository.GetAllByMotoristaAsync(1, 7).Returns(new[] { RotaAberta(veiculoId: 1) });

            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida()));

            await _repository.Received(1).AddAsync(Arg.Is<Abastecimento>(a => a.RotaId == 42 && a.UsuarioId == 10));
        }

        [Fact]
        public async Task Create_ComoGestao_ComRotaAbertaDoMotoristaEmOutroVeiculo_DeveLancarSemRota()
        {
            PrepararCreate();
            _rotaRepository.GetAllByMotoristaAsync(1, 7).Returns(new[] { RotaAberta(veiculoId: 2) });

            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida()));

            await _repository.Received(1).AddAsync(Arg.Is<Abastecimento>(a => a.RotaId == null));
        }

        [Fact]
        public async Task Create_DeveRegistrarAuditoria()
        {
            PrepararCreate();

            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida()));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Abastecimento, AcoesAuditoria.Criou, 5,
                Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>>());
        }

        // ---------- Segundo eixo: o motorista ----------

        [Fact]
        public async Task GetAll_ComoMotorista_DeveRecortarPeloProprioMotorista()
        {
            ComoMotorista(7);
            _repository.GetAllAsync(1, null, 7, null, null).Returns([]);

            await CriarGetAllHandler().HandleAsync(new GetAllAbastecimentosQuery());

            await _repository.Received(1).GetAllAsync(1, null, 7, null, null);
        }

        /// <summary>O filtro do cliente não vale para o motorista: o recorte sai sempre do token.</summary>
        [Fact]
        public async Task GetAll_ComoMotorista_DeveIgnorarOFiltroDeMotoristaDoCliente()
        {
            ComoMotorista(7);
            _repository.GetAllAsync(1, null, 7, null, null).Returns([]);

            await CriarGetAllHandler().HandleAsync(new GetAllAbastecimentosQuery(MotoristaId: 9));

            await _repository.Received(1).GetAllAsync(1, null, 7, null, null);
        }

        [Fact]
        public async Task GetAll_ComoGestao_SemFiltro_NaoDeveRecortarPorMotorista()
        {
            _repository.GetAllAsync(1, null, null, null, null).Returns([]);

            await CriarGetAllHandler().HandleAsync(new GetAllAbastecimentosQuery());

            await _repository.Received(1).GetAllAsync(1, null, null, null, null);
        }

        [Fact]
        public async Task GetAll_ComoGestao_ComFiltroDeMotorista_DeveRepassar()
        {
            _repository.GetAllAsync(1, null, 7, null, null).Returns([]);

            await CriarGetAllHandler().HandleAsync(new GetAllAbastecimentosQuery(MotoristaId: 7));

            await _repository.Received(1).GetAllAsync(1, null, 7, null, null);
        }

        [Fact]
        public async Task GetAll_ComPeriodoInvertido_DeveLancar()
        {
            var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CriarGetAllHandler().HandleAsync(new GetAllAbastecimentosQuery(
                    De: new DateTime(2026, 8, 28), Ate: new DateTime(2026, 8, 1))));

            Assert.Equal("A data final do período não pode ser anterior à inicial.", erro.Message);
        }

        [Fact]
        public async Task GetById_ComoMotorista_LancamentoDeOutro_DeveRetornarNull()
        {
            // 404 e não 403: para quem não é dono do gasto, o registro não existe.
            ComoMotorista(7);
            _repository.GetByIdAsync(5, 1).Returns(NovoAbastecimento(5, motoristaId: 9));

            var handler = new GetAbastecimentoByIdHandler(_repository, _currentUser,
                NullLogger<GetAbastecimentoByIdHandler>.Instance);

            Assert.Null(await handler.HandleAsync(new GetAbastecimentoByIdQuery(5)));
        }

        /// <summary>
        /// O recorte é por motorista, não por quem digitou: o lançamento que o supervisor fez
        /// <b>para</b> ele é dele.
        /// </summary>
        [Fact]
        public async Task GetById_ComoMotorista_LancamentoQueAGestaoFezParaEle_DeveRetornar()
        {
            ComoMotorista(7);
            _repository.GetByIdAsync(5, 1).Returns(NovoAbastecimento(5, motoristaId: 7, usuarioId: 10));

            var handler = new GetAbastecimentoByIdHandler(_repository, _currentUser,
                NullLogger<GetAbastecimentoByIdHandler>.Instance);

            var resposta = await handler.HandleAsync(new GetAbastecimentoByIdQuery(5));

            Assert.NotNull(resposta);
            Assert.Equal(7, resposta.MotoristaId);
            Assert.Equal(10, resposta.UsuarioId);
        }

        [Fact]
        public async Task Update_ComoMotorista_LancamentoDeOutro_DeveRetornarNullENaoGravar()
        {
            ComoMotorista(7);
            PrepararUpdate();
            _repository.GetByIdAsync(5, 1).Returns(NovoAbastecimento(5, motoristaId: 9));

            var resposta = await CriarUpdateHandler().HandleAsync(
                new UpdateAbastecimentoCommand(5, CorpoDeCorrecao()));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Abastecimento>());
        }

        [Fact]
        public async Task Update_ComoMotorista_ProprioLancamento_DeveCorrigirERegistrarAuditoria()
        {
            ComoMotorista(7);
            PrepararUpdate();
            _repository.GetByIdAsync(5, 1).Returns(NovoAbastecimento(5, motoristaId: 7, valor: 320m));

            // 55 L a R$ 6,40 = R$ 352,00 — o total vem do cálculo, não do corpo.
            await CriarUpdateHandler().HandleAsync(
                new UpdateAbastecimentoCommand(5, CorpoDeCorrecao(litros: 55m)));

            await _repository.Received(1).UpdateAsync(Arg.Is<Abastecimento>(a => a.Valor == 352m));
            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Abastecimento, AcoesAuditoria.Atualizou, 5,
                Arg.Any<string>(),
                Arg.Is<IEnumerable<AlteracaoCampo>>(d => d.Any(c => c.Campo == "Valor")));
        }

        // ---------- Apontamento fiscal ----------

        [Fact]
        public async Task Create_DeveCalcularOValorAPartirDeLitrosEValorLitro()
        {
            PrepararCreate();

            // 48,5 L a R$ 6,19 = R$ 300,215 -> 300,22. O corpo nem carrega o total.
            var requisicao = RequisicaoValida();
            requisicao.Litros = 48.5m;
            requisicao.ValorLitro = 6.19m;

            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(requisicao));

            await _repository.Received(1).AddAsync(Arg.Is<Abastecimento>(a => a.Valor == 300.22m));
        }

        [Fact]
        public async Task Create_ComOdometroMaiorQueODoVeiculo_DeveAvancarAQuilometragem()
        {
            PrepararCreate();

            var requisicao = RequisicaoValida();
            requisicao.Odometro = 61_500;

            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(requisicao));

            await _veiculoRepository.Received(1).UpdateAsync(Arg.Is<Veiculo>(v => v.Quilometragem == 61_500));
        }

        [Fact]
        public async Task Create_ComOdometroMenorQueODoVeiculo_NaoDeveRetroceder()
        {
            PrepararCreate();

            // O veiculo esta em 60.000: um lancamento retroativo nao reescreve a ficha.
            var requisicao = RequisicaoValida();
            requisicao.Odometro = 55_000;

            await CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(requisicao));

            await _veiculoRepository.DidNotReceive().UpdateAsync(Arg.Any<Veiculo>());
        }

        [Fact]
        public async Task Create_ComTipoCombustivelDeOutraEmpresa_DeveLancar()
        {
            PrepararCreate();
            _tipoCombustivelRepository.GetByIdAsync(3, 1).Returns((TipoCombustivel?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida())));
        }

        [Fact]
        public async Task Create_ComPostoDeOutraEmpresa_DeveLancar()
        {
            PrepararCreate();
            _postoRepository.GetByIdAsync(4, 1).Returns((Posto?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida())));
        }

        [Fact]
        public async Task Create_ComCombustivelInativo_DeveLancar()
        {
            PrepararCreate();
            _tipoCombustivelRepository.GetByIdAsync(3, 1).Returns(NovoTipoCombustivel(ativo: false));

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida())));

            Assert.Contains("inativo", erro.Message);
        }

        [Fact]
        public async Task Create_ComPostoDescredenciado_DeveLancar()
        {
            PrepararCreate();
            _postoRepository.GetByIdAsync(4, 1).Returns(NovoPosto(ativo: false));

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarCreateHandler().HandleAsync(new CreateAbastecimentoCommand(RequisicaoValida())));

            Assert.Contains("descredenciado", erro.Message);
        }

        [Fact]
        public async Task Update_ComOdometroMaior_DeveAvancarAQuilometragemDoVeiculo()
        {
            PrepararUpdate();
            _repository.GetByIdAsync(5, 1).Returns(NovoAbastecimento(5));

            await CriarUpdateHandler().HandleAsync(
                new UpdateAbastecimentoCommand(5, CorpoDeCorrecao(odometro: 62_000)));

            await _veiculoRepository.Received(1).UpdateAsync(Arg.Is<Veiculo>(v => v.Quilometragem == 62_000));
        }

        [Fact]
        public async Task Update_ComOdometroMenor_NaoDeveRetrocederAQuilometragem()
        {
            PrepararUpdate();
            _repository.GetByIdAsync(5, 1).Returns(NovoAbastecimento(5));

            await CriarUpdateHandler().HandleAsync(
                new UpdateAbastecimentoCommand(5, CorpoDeCorrecao(odometro: 50_000)));

            await _veiculoRepository.DidNotReceive().UpdateAsync(Arg.Any<Veiculo>());
        }

        // ---------- Delete ----------

        [Fact]
        public async Task Delete_QuandoExiste_DeveRemoverERegistrarAuditoria()
        {
            var existente = NovoAbastecimento(5);
            _repository.GetByIdAsync(5, 1).Returns(existente);

            var handler = new DeleteAbastecimentoHandler(_repository, _currentUser, _auditoria,
                NullLogger<DeleteAbastecimentoHandler>.Instance);

            Assert.True(await handler.HandleAsync(new DeleteAbastecimentoCommand(5)));

            await _repository.Received(1).DeleteAsync(existente);
            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Abastecimento, AcoesAuditoria.Excluiu, 5,
                Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>>());
        }

        [Fact]
        public async Task Delete_QuandoNaoExiste_DeveRetornarFalseENaoAuditar()
        {
            _repository.GetByIdAsync(123, 1).Returns((Abastecimento?)null);

            var handler = new DeleteAbastecimentoHandler(_repository, _currentUser, _auditoria,
                NullLogger<DeleteAbastecimentoHandler>.Instance);

            Assert.False(await handler.HandleAsync(new DeleteAbastecimentoCommand(123)));

            await _auditoria.DidNotReceive().RegistrarAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>>());
        }
    }
}
