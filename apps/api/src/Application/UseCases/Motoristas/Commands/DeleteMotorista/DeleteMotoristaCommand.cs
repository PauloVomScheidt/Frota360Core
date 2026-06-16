using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.Motoristas.Commands.DeleteMotorista
{
    public sealed record DeleteMotoristaCommand(int Id) : ICommand<bool>;
}
