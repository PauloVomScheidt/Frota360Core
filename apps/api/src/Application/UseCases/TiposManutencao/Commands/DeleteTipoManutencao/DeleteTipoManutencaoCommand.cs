using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.TiposManutencao.Commands.DeleteTipoManutencao
{
    public sealed record DeleteTipoManutencaoCommand(int Id) : ICommand<bool>;
}
