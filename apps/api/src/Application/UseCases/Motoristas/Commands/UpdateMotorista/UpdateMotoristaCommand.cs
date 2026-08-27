using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Request;
using Frota360.Application.DTOs.Motorista.Response;

namespace Frota360.Application.UseCases.Motoristas.Commands.UpdateMotorista
{
    public sealed record UpdateMotoristaCommand(int Id, UpdateMotoristaRequest Data) : ICommand<MotoristaResponse?>;
}
