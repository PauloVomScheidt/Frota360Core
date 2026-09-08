using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.TiposCombustivel.Commands.DeleteTipoCombustivel
{
    public sealed record DeleteTipoCombustivelCommand(int Id) : ICommand<bool>;
}
