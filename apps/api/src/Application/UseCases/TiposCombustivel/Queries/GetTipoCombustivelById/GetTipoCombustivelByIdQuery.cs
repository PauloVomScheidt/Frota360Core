using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoCombustivel.Response;

namespace Frota360.Application.UseCases.TiposCombustivel.Queries.GetTipoCombustivelById
{
    public sealed record GetTipoCombustivelByIdQuery(int Id) : IQuery<TipoCombustivelResponse?>;
}
