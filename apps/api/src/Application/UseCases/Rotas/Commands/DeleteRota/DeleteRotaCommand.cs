using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.Rotas.Commands.DeleteRota
{
    public sealed record DeleteRotaCommand(int Id) : ICommand<bool>;
}
