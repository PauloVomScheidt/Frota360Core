using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Request;
using Frota360.Application.DTOs.Manutencao.Response;

namespace Frota360.Application.UseCases.Manutencoes.Commands.UpdateManutencao
{
    public sealed record UpdateManutencaoCommand(int Id, UpdateManutencaoRequest Data) : ICommand<ManutencaoResponse?>;
}
