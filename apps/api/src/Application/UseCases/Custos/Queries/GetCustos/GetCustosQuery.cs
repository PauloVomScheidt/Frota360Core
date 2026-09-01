using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Custo.Request;
using Frota360.Application.DTOs.Custo.Response;
using Frota360.Domain.Common;

namespace Frota360.Application.UseCases.Custos.Queries.GetCustos
{
    public sealed record GetCustosQuery(ConsultarCustosRequest Data)
        : IQuery<ResultadoPaginado<LancamentoCustoResponse>>;
}
