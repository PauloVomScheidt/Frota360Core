using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Request;
using Frota360.Application.DTOs.Motorista.Response;

namespace Frota360.Application.UseCases.Motoristas.Commands.CreateMotorista
{
    public sealed record CreateMotoristaCommand(CreateMotoristaRequest Data) : ICommand<MotoristaResponse>;
}
