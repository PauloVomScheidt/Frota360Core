using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;

namespace Frota360.Application.UseCases.Rotas.Queries.GetAllRotas
{
    public sealed record GetAllRotasQuery : IQuery<IEnumerable<RotaResponse>>;
}
