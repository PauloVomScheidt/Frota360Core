using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Response;

namespace Frota360.Application.UseCases.Manutencoes.Queries.GetManutencaoById
{
    public sealed record GetManutencaoByIdQuery(int Id) : IQuery<ManutencaoResponse?>;
}
