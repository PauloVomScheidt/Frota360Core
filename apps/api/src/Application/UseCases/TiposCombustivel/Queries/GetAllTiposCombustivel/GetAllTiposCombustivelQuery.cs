using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoCombustivel.Response;

namespace Frota360.Application.UseCases.TiposCombustivel.Queries.GetAllTiposCombustivel
{
    public sealed record GetAllTiposCombustivelQuery(bool ApenasAtivos = false) : IQuery<IEnumerable<TipoCombustivelResponse>>;
}
