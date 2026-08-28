using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Auditoria.Request;
using Frota360.Application.DTOs.Auditoria.Response;
using Frota360.Domain.Common;

namespace Frota360.Application.UseCases.Auditoria.Queries.GetLogsAuditoria
{
    public sealed record GetLogsAuditoriaQuery(ConsultarAuditoriaRequest Filtro)
        : IQuery<ResultadoPaginado<LogAuditoriaResponse>>;
}
