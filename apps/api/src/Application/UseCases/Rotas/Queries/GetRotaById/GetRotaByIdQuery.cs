using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;

namespace Frota360.Application.UseCases.Rotas.Queries.GetRotaById
{
    public sealed record GetRotaByIdQuery(int Id) : IQuery<RotaResponse?>;
}
