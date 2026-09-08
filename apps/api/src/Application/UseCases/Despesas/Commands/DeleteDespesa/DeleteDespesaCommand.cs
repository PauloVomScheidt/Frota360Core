using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.Despesas.Commands.DeleteDespesa
{
    public sealed record DeleteDespesaCommand(int Id) : ICommand<bool>;
}
