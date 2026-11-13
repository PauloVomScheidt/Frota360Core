using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.TiposDespesa.Commands.DeleteTipoDespesa
{
    public sealed record DeleteTipoDespesaCommand(int Id) : ICommand<bool>;
}
