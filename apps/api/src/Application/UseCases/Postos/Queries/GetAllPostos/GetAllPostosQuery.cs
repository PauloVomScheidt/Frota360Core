using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Posto.Response;

namespace Frota360.Application.UseCases.Postos.Queries.GetAllPostos
{
    public sealed record GetAllPostosQuery(bool ApenasAtivos = false) : IQuery<IEnumerable<PostoResponse>>;
}
