using Frota360.Application.DTOs.Despesa.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Despesas.Commands.CreateDespesa;
using Frota360.Application.UseCases.Despesas.Commands.DeleteDespesa;
using Frota360.Application.UseCases.Despesas.Commands.UpdateDespesa;
using Frota360.Application.UseCases.Despesas.Queries.GetAllDespesas;
using Frota360.Application.UseCases.Despesas.Queries.GetResumoDespesas;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Despesas
{
    public class DespesaHandlersTests
    {
        private readonly IDespesaRepository _repository = Substitute.For<IDespesaRepository>();
        private readonly IVeiculoRepository _veiculoRepository = Substitute.For<IVeiculoRepository>();
        private readonly ITipoDespesaRepository _tipoRepository = Substitute.For<ITipoDespesaRepository>();
        private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();

        public DespesaHandlersTests()
        {
            _currentUser.EmpresaId.Returns(1);
            _currentUser.UsuarioId.Returns(10);
            _currentUser.Role.Returns(Roles.Supervisor);
        }

        private CreateDespesaHandler CriarCreateHandler() =>
            new(_repository, _veiculoRepository, _tipoRepository, _usuarioRepository,
                _currentUser, _auditoria, NullLogger<CreateDespesaHandler>.Instance);

        private UpdateDespesaHandler CriarUpdateHandler() =>
            new(_repository, _veiculoRepository, _tipoRepository, _usuarioRepository,
                _currentUser, _auditoria, NullLogger<UpdateDespesaHandler>.Instance);

        private DeleteDespesaHandler CriarDeleteHandler() =>
            new(_repository, _currentUser, _auditoria, NullLogger<DeleteDespesaHandler>.Instance);

        private GetAllDespesasHandler CriarGetAllHandler() =>
            new(_repository, _currentUser, NullLogger<GetAllDespesasHandler>.Instance);

        private static ConsultarDespesasRequest Consulta(int pagina = 1, int tamanhoPagina = 15,
            int? veiculoId = null, int? motoristaId = null, int? tipoDespesaId = null,
            DateTime? de = null, DateTime? ate = null) => new()
        {
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
            VeiculoId = veiculoId,
            MotoristaId = motoristaId,
            TipoDespesaId = tipoDespesaId,
            De = de,
            Ate = ate
        };

        private static Veiculo NovoVeiculo(int id = 5) => new()
        {
            Id = id,
            EmpresaId = 1,
            NomeVeiculo = "Strada",
            MarcaVeiculo = "Fiat",
            Placa = "ABC1D23"
        };

        private static TipoDespesa NovoTipo(int id = 3, string nome = "Pedágio", bool ativo = true) =>
            new() { Id = id, EmpresaId = 1, Nome = nome, Ativo = ativo };

        private static Usuario NovoMotorista(int id = 7, string nome = "João Lima") =>
            new() { Id = id, EmpresaId = 1, Nome = nome, Role = Roles.Motorista };

        private static Despesa NovaDespesa(int id = 1, decimal valor = 100m, int? motoristaId = null) => new()
        {
            Id = id,
            EmpresaId = 1,
            VeiculoId = 5,
            TipoDespesaId = 3,
            MotoristaId = motoristaId,
            Valor = valor,
            DataDespesa = new DateTime(2026, 9, 1),
            Veiculo = NovoVeiculo(),
            Tipo = NovoTipo(),
            Motorista = motoristaId is null ? null : NovoMotorista(motoristaId.Value)
        };

        private static CreateDespesaRequest NovoRequest(int? motoristaId = null) => new()
        {
            VeiculoId = 5,
            TipoDespesaId = 3,
            MotoristaId = motoristaId,
            Valor = 100m,
            DataDespesa = new DateTime(2026, 9, 1)
        };

        /// <summary>Caminho feliz do create: veículo e tipo existem e estão em ordem.</summary>
        private void ComVeiculoETipo(bool tipoAtivo = true)
        {
            _veiculoRepository.GetByIdAsync(5, 1).Returns(NovoVeiculo());
            _tipoRepository.GetByIdAsync(3, 1).Returns(NovoTipo(ativo: tipoAtivo));
            _repository.AddAsync(Arg.Any<Despesa>()).Returns(c => c.Arg<Despesa>());
            _repository.UpdateAsync(Arg.Any<Despesa>()).Returns(c => c.Arg<Despesa>());
        }

        [Fact]
        public async Task Create_DevePersistirEscopadoNaEmpresaEResolverAsFksComEmpresaId()
        {
            ComVeiculoETipo();

            var handler = CriarCreateHandler();
            await handler.HandleAsync(new CreateDespesaCommand(NovoRequest()));

            // Toda FK do request é resolvida com o empresaId do token — id de outra empresa
            // simplesmente "não existe".
            await _veiculoRepository.Received(1).GetByIdAsync(5, 1);
            await _tipoRepository.Received(1).GetByIdAsync(3, 1);
            await _repository.Received(1).AddAsync(Arg.Is<Despesa>(d => d.EmpresaId == 1));
        }

        [Fact]
        public async Task Create_DeveMapearARespostaDesnormalizada()
        {
            ComVeiculoETipo();
            _usuarioRepository.GetMotoristaByIdAsync(7, 1).Returns(NovoMotorista());

            var handler = CriarCreateHandler();
            var resposta = await handler.HandleAsync(new CreateDespesaCommand(NovoRequest(motoristaId: 7)));

            Assert.Equal("ABC1D23", resposta.VeiculoPlaca);
            Assert.Equal("Pedágio", resposta.TipoDespesaNome);
            Assert.Equal("João Lima", resposta.MotoristaNome);
            Assert.Equal(100m, resposta.Valor);
        }

        [Fact]
        public async Task Create_SemMotorista_DeveGravarSemDono()
        {
            // IPVA e seguro não são de ninguém em particular.
            ComVeiculoETipo();

            var handler = CriarCreateHandler();
            var resposta = await handler.HandleAsync(new CreateDespesaCommand(NovoRequest()));

            Assert.Null(resposta.MotoristaId);
            Assert.Null(resposta.MotoristaNome);
            await _usuarioRepository.DidNotReceive().GetMotoristaByIdAsync(Arg.Any<int>(), Arg.Any<int>());
        }

        [Fact]
        public async Task Create_ComVeiculoDeOutraEmpresa_DeveRecusar()
        {
            _veiculoRepository.GetByIdAsync(5, 1).Returns((Veiculo?)null);

            var handler = CriarCreateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateDespesaCommand(NovoRequest())));
        }

        [Fact]
        public async Task Create_ComTipoDeOutraEmpresa_DeveRecusar()
        {
            _veiculoRepository.GetByIdAsync(5, 1).Returns(NovoVeiculo());
            _tipoRepository.GetByIdAsync(3, 1).Returns((TipoDespesa?)null);

            var handler = CriarCreateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateDespesaCommand(NovoRequest())));
        }

        [Fact]
        public async Task Create_ComMotoristaDeOutraEmpresa_DeveRecusar()
        {
            // GetMotoristaByIdAsync filtra empresa e role: um Supervisor informado aqui
            // também cai neste caminho.
            ComVeiculoETipo();
            _usuarioRepository.GetMotoristaByIdAsync(7, 1).Returns((Usuario?)null);

            var handler = CriarCreateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateDespesaCommand(NovoRequest(motoristaId: 7))));
        }

        [Fact]
        public async Task Create_ComTipoInativo_DeveRecusar()
        {
            ComVeiculoETipo(tipoAtivo: false);

            var handler = CriarCreateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateDespesaCommand(NovoRequest())));
        }

        [Fact]
        public async Task Create_DeveRegistrarAuditoria()
        {
            ComVeiculoETipo();

            var handler = CriarCreateHandler();
            await handler.HandleAsync(new CreateDespesaCommand(NovoRequest()));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Despesa, AcoesAuditoria.Criou,
                Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<IEnumerable<Frota360.Application.Common.AlteracaoCampo>?>());
        }

        [Fact]
        public async Task Update_DeveEscoparNaEmpresaEAlterarTodosOsCampos()
        {
            // Ao contrário do abastecimento, aqui veículo, tipo e motorista são editáveis:
            // não há recorte por dono que a troca burlaria.
            _repository.GetByIdAsync(1, 1).Returns(NovaDespesa());
            _veiculoRepository.GetByIdAsync(9, 1).Returns(NovoVeiculo(9));
            _tipoRepository.GetByIdAsync(4, 1).Returns(NovoTipo(4, "IPVA"));
            _repository.UpdateAsync(Arg.Any<Despesa>()).Returns(c => c.Arg<Despesa>());

            var handler = CriarUpdateHandler();
            var resposta = await handler.HandleAsync(new UpdateDespesaCommand(1, new UpdateDespesaRequest
            {
                VeiculoId = 9,
                TipoDespesaId = 4,
                Valor = 900m,
                DataDespesa = new DateTime(2026, 9, 2)
            }));

            await _repository.Received(1).GetByIdAsync(1, 1);
            Assert.NotNull(resposta);
            Assert.Equal(9, resposta.VeiculoId);
            Assert.Equal(4, resposta.TipoDespesaId);
            Assert.Equal(900m, resposta.Valor);
        }

        [Fact]
        public async Task Update_MantendoOMesmoTipoInativo_NaoDeveRecusar()
        {
            // Um tipo aposentado depois do lançamento não pode travar a correção do valor.
            _repository.GetByIdAsync(1, 1).Returns(NovaDespesa());
            _veiculoRepository.GetByIdAsync(5, 1).Returns(NovoVeiculo());
            _tipoRepository.GetByIdAsync(3, 1).Returns(NovoTipo(ativo: false));
            _repository.UpdateAsync(Arg.Any<Despesa>()).Returns(c => c.Arg<Despesa>());

            var handler = CriarUpdateHandler();
            var resposta = await handler.HandleAsync(new UpdateDespesaCommand(1, new UpdateDespesaRequest
            {
                VeiculoId = 5,
                TipoDespesaId = 3,
                Valor = 250m,
                DataDespesa = new DateTime(2026, 9, 1)
            }));

            Assert.NotNull(resposta);
            Assert.Equal(250m, resposta.Valor);
        }

        [Fact]
        public async Task Update_TrocandoParaTipoInativo_DeveRecusar()
        {
            _repository.GetByIdAsync(1, 1).Returns(NovaDespesa());
            _veiculoRepository.GetByIdAsync(5, 1).Returns(NovoVeiculo());
            _tipoRepository.GetByIdAsync(4, 1).Returns(NovoTipo(4, "Lavagem", ativo: false));

            var handler = CriarUpdateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new UpdateDespesaCommand(1, new UpdateDespesaRequest
                {
                    VeiculoId = 5,
                    TipoDespesaId = 4,
                    Valor = 250m,
                    DataDespesa = new DateTime(2026, 9, 1)
                })));
        }

        [Fact]
        public async Task Update_DeveRegistrarODiffNaAuditoria()
        {
            _repository.GetByIdAsync(1, 1).Returns(NovaDespesa());
            _veiculoRepository.GetByIdAsync(5, 1).Returns(NovoVeiculo());
            _tipoRepository.GetByIdAsync(3, 1).Returns(NovoTipo());
            _repository.UpdateAsync(Arg.Any<Despesa>()).Returns(c => c.Arg<Despesa>());

            var handler = CriarUpdateHandler();
            await handler.HandleAsync(new UpdateDespesaCommand(1, new UpdateDespesaRequest
            {
                VeiculoId = 5,
                TipoDespesaId = 3,
                Valor = 777m,
                DataDespesa = new DateTime(2026, 9, 1)
            }));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Despesa, AcoesAuditoria.Atualizou, 1, Arg.Any<string>(),
                Arg.Is<IEnumerable<Frota360.Application.Common.AlteracaoCampo>?>(
                    a => a != null && a.Any(c => c.Campo == "Valor")));
        }

        [Fact]
        public async Task Update_DespesaInexistente_DeveDevolverNuloSemAuditar()
        {
            _repository.GetByIdAsync(1, 1).Returns((Despesa?)null);

            var handler = CriarUpdateHandler();
            var resposta = await handler.HandleAsync(new UpdateDespesaCommand(1, new UpdateDespesaRequest
            {
                VeiculoId = 5,
                TipoDespesaId = 3,
                Valor = 100m,
                DataDespesa = new DateTime(2026, 9, 1)
            }));

            Assert.Null(resposta);
            await _auditoria.DidNotReceive().RegistrarAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(),
                Arg.Any<IEnumerable<Frota360.Application.Common.AlteracaoCampo>?>());
        }

        [Fact]
        public async Task Delete_DeveEscoparNaEmpresaERegistrarAuditoria()
        {
            _repository.GetByIdAsync(1, 1).Returns(NovaDespesa());

            var handler = CriarDeleteHandler();
            var removida = await handler.HandleAsync(new DeleteDespesaCommand(1));

            Assert.True(removida);
            await _repository.Received(1).GetByIdAsync(1, 1);
            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Despesa, AcoesAuditoria.Excluiu, 1, Arg.Any<string>(),
                Arg.Any<IEnumerable<Frota360.Application.Common.AlteracaoCampo>?>());
        }

        [Fact]
        public async Task GetAll_DeveEscoparNaEmpresaERepassarOFiltro()
        {
            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroDespesa>())
                .Returns((new[] { NovaDespesa() }, 1));

            var handler = CriarGetAllHandler();
            await handler.HandleAsync(new GetAllDespesasQuery(Consulta(
                veiculoId: 5, motoristaId: 7, tipoDespesaId: 3,
                de: new DateTime(2026, 9, 1), ate: new DateTime(2026, 9, 30))));

            await _repository.Received(1).ConsultarAsync(1, Arg.Is<FiltroDespesa>(f =>
                f.VeiculoId == 5 && f.MotoristaId == 7 && f.TipoDespesaId == 3 &&
                f.De == new DateTime(2026, 9, 1) && f.Ate == new DateTime(2026, 9, 30)));
        }

        [Fact]
        public async Task GetAll_DeveRepassarAPaginacaoEEcoarNaResposta()
        {
            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroDespesa>())
                .Returns((new[] { NovaDespesa() }, 42));

            var pagina = await CriarGetAllHandler().HandleAsync(
                new GetAllDespesasQuery(Consulta(pagina: 2, tamanhoPagina: 10)));

            await _repository.Received(1).ConsultarAsync(1,
                Arg.Is<FiltroDespesa>(f => f.Pagina == 2 && f.TamanhoPagina == 10));
            Assert.Equal(2, pagina.Pagina);
            Assert.Equal(10, pagina.TamanhoPagina);
            Assert.Equal(42, pagina.Total);   // o total ignora a paginação
        }

        [Fact]
        public async Task GetResumo_DeveUsarOMesmoFiltroDaListagem()
        {
            // Se lista e rodapé divergirem, a tela soma um conjunto que a tabela não mostra.
            _repository.ResumirAsync(Arg.Any<int>(), Arg.Any<FiltroDespesa>())
                .Returns(new ResumoLancamentos(4, 1200m));

            var handler = new GetResumoDespesasHandler(_repository, _currentUser,
                NullLogger<GetResumoDespesasHandler>.Instance);

            var resumo = await handler.HandleAsync(new GetResumoDespesasQuery(Consulta(veiculoId: 5)));

            Assert.Equal(4, resumo.Quantidade);
            Assert.Equal(1200m, resumo.ValorTotal);
            await _repository.Received(1).ResumirAsync(1, Arg.Is<FiltroDespesa>(f => f.VeiculoId == 5));
        }

        [Fact]
        public async Task GetAll_ComPeriodoInvertido_DeveRecusar()
        {
            var handler = CriarGetAllHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new GetAllDespesasQuery(Consulta(
                    de: new DateTime(2026, 9, 30), ate: new DateTime(2026, 9, 1)))));
        }
    }
}
