using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Request;
using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Domain.Common;

namespace Frota360.Application.UseCases.Despesas.Queries.GetAllDespesas
{
    public sealed record GetAllDespesasQuery(ConsultarDespesasRequest Filtro)
        : IQuery<ResultadoPaginado<DespesaResponse>>;
}
