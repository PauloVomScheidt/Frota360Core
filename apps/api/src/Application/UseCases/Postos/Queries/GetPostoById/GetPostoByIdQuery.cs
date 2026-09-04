using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Posto.Response;

namespace Frota360.Application.UseCases.Postos.Queries.GetPostoById
{
    public sealed record GetPostoByIdQuery(int Id) : IQuery<PostoResponse?>;
}
