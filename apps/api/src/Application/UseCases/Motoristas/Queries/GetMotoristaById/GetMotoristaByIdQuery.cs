using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Response;

namespace Frota360.Application.UseCases.Motoristas.Queries.GetMotoristaById
{
    public sealed record GetMotoristaByIdQuery(int Id) : IQuery<MotoristaResponse?>;
}
