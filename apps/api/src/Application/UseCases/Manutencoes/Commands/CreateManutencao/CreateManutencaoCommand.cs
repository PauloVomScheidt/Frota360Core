using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Request;
using Frota360.Application.DTOs.Manutencao.Response;

namespace Frota360.Application.UseCases.Manutencoes.Commands.CreateManutencao
{
    public sealed record CreateManutencaoCommand(CreateManutencaoRequest Data) : ICommand<ManutencaoResponse>;
}
