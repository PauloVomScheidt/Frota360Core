using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoManutencao.Response;

namespace Frota360.Application.UseCases.TiposManutencao.Queries.GetTipoManutencaoById
{
    public sealed record GetTipoManutencaoByIdQuery(int Id) : IQuery<TipoManutencaoResponse?>;
}
