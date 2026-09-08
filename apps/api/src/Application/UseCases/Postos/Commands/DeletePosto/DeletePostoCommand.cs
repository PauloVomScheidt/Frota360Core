using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.Postos.Commands.DeletePosto
{
    public sealed record DeletePostoCommand(int Id) : ICommand<bool>;
}
