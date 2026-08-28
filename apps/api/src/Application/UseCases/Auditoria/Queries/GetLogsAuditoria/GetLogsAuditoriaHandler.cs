using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Auditoria.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Auditoria.Queries.GetLogsAuditoria
{
    public sealed class GetLogsAuditoriaHandler(ILogAuditoriaRepository repository,
                                                ICurrentUserService currentUser,
                                                ILogger<GetLogsAuditoriaHandler> logger)
        : IQueryHandler<GetLogsAuditoriaQuery, ResultadoPaginado<LogAuditoriaResponse>>
    {
        public async Task<ResultadoPaginado<LogAuditoriaResponse>> HandleAsync(
            GetLogsAuditoriaQuery query, CancellationToken cancellationToken = default)
        {
            var f = query.Filtro;

            logger.LogInformation("Consultando auditoria | Página {Pagina} | Entidade {Entidade} | Ação {Acao} | Usuário {UsuarioId}",
                f.Pagina, f.Entidade, f.Acao, f.UsuarioId);

            var filtro = new FiltroLogAuditoria(
                f.Pagina, f.TamanhoPagina, f.Entidade, f.Acao, f.UsuarioId, f.De, f.Ate);

            var (itens, total) = await repository.ConsultarAsync(currentUser.EmpresaId, filtro);

            logger.LogInformation("Auditoria consultada. {Quantidade} registros na página, {Total} no total", itens.Count(), total);

            return new ResultadoPaginado<LogAuditoriaResponse>
            {
                Itens = itens.Select(l => l.ToResponse()),
                Pagina = f.Pagina,
                TamanhoPagina = f.TamanhoPagina,
                Total = total
            };
        }
    }
}
