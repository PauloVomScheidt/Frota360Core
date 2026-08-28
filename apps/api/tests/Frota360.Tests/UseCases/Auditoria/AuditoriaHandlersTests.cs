using Frota360.Application.DTOs.Auditoria.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Auditoria.Queries.GetLogsAuditoria;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Auditoria
{
    public class AuditoriaHandlersTests
    {
        private readonly ILogAuditoriaRepository _repository = Substitute.For<ILogAuditoriaRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

        public AuditoriaHandlersTests()
        {
            _currentUser.EmpresaId.Returns(1);
        }

        private GetLogsAuditoriaHandler CriarHandler() =>
            new(_repository, _currentUser, NullLogger<GetLogsAuditoriaHandler>.Instance);

        private static LogAuditoria NovoLog(long id = 1, string? alteracoes = null) => new()
        {
            Id = id,
            EmpresaId = 1,
            UsuarioId = 10,
            UsuarioNome = "Ana Souza",
            UsuarioEmail = "ana@empresa.com",
            UsuarioRole = Roles.Admin,
            Entidade = EntidadesAuditadas.Veiculo,
            Acao = AcoesAuditoria.Atualizou,
            EntidadeId = 7,
            Descricao = "Atualizou o veículo ABC1D23",
            Alteracoes = alteracoes,
            DataHora = new DateTime(2026, 8, 28, 14, 30, 0, DateTimeKind.Utc)
        };

        [Fact]
        public async Task Consultar_DeveEscoparNaEmpresaERepassarOFiltro()
        {
            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroLogAuditoria>())
                .Returns(([NovoLog()], 1));

            var handler = CriarHandler();

            await handler.HandleAsync(new GetLogsAuditoriaQuery(new ConsultarAuditoriaRequest
            {
                Pagina = 2,
                TamanhoPagina = 25,
                Entidade = EntidadesAuditadas.Rota,
                Acao = AcoesAuditoria.Encerrou,
                UsuarioId = 10
            }));

            await _repository.Received(1).ConsultarAsync(1, Arg.Is<FiltroLogAuditoria>(f =>
                f.Pagina == 2
                && f.TamanhoPagina == 25
                && f.Entidade == EntidadesAuditadas.Rota
                && f.Acao == AcoesAuditoria.Encerrou
                && f.UsuarioId == 10));
        }

        [Fact]
        public async Task Consultar_DeveMontarAPaginaComTotalETotalDePaginas()
        {
            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroLogAuditoria>())
                .Returns(([NovoLog(1), NovoLog(2)], 57));

            var handler = CriarHandler();

            var pagina = await handler.HandleAsync(new GetLogsAuditoriaQuery(
                new ConsultarAuditoriaRequest { Pagina = 1, TamanhoPagina = 25 }));

            Assert.Equal(2, pagina.Itens.Count());
            Assert.Equal(57, pagina.Total);
            Assert.Equal(1, pagina.Pagina);
            Assert.Equal(25, pagina.TamanhoPagina);
            Assert.Equal(3, pagina.TotalPaginas); // 57 em páginas de 25
        }

        [Fact]
        public async Task Consultar_DeveDesserializarODiffParaListaTipada()
        {
            const string json = """[{"Campo":"Placa","De":"ABC1D23","Para":"XYZ9K87"}]""";

            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroLogAuditoria>())
                .Returns(([NovoLog(alteracoes: json)], 1));

            var handler = CriarHandler();

            var pagina = await handler.HandleAsync(new GetLogsAuditoriaQuery(new ConsultarAuditoriaRequest()));

            var log = Assert.Single(pagina.Itens);
            var alteracao = Assert.Single(log.Alteracoes);
            Assert.Equal("Placa", alteracao.Campo);
            Assert.Equal("ABC1D23", alteracao.De);
            Assert.Equal("XYZ9K87", alteracao.Para);
        }

        /// <summary>
        /// Uma linha com JSON corrompido não pode derrubar a listagem inteira — a trilha é
        /// histórico, e perder o resto por causa de um registro seria pior que a falha.
        /// </summary>
        [Fact]
        public async Task Consultar_ComDiffCorrompido_DeveDevolverListaVaziaSemQuebrar()
        {
            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroLogAuditoria>())
                .Returns(([NovoLog(alteracoes: "{isso não é json válido")], 1));

            var handler = CriarHandler();

            var pagina = await handler.HandleAsync(new GetLogsAuditoriaQuery(new ConsultarAuditoriaRequest()));

            Assert.Empty(Assert.Single(pagina.Itens).Alteracoes);
        }

        [Fact]
        public async Task Consultar_DeveMapearOsDadosDesnormalizadosDoAtor()
        {
            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroLogAuditoria>())
                .Returns(([NovoLog()], 1));

            var handler = CriarHandler();

            var pagina = await handler.HandleAsync(new GetLogsAuditoriaQuery(new ConsultarAuditoriaRequest()));

            var log = Assert.Single(pagina.Itens);
            Assert.Equal("Ana Souza", log.UsuarioNome);
            Assert.Equal("ana@empresa.com", log.UsuarioEmail);
            Assert.Equal(Roles.Admin, log.UsuarioRole);
            Assert.Equal("Atualizou o veículo ABC1D23", log.Descricao);
            Assert.Equal(7, log.EntidadeId);
        }
    }
}
