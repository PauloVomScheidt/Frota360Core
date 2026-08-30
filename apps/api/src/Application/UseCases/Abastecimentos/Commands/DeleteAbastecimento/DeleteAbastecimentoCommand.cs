using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.Abastecimentos.Commands.DeleteAbastecimento
{
    public sealed record DeleteAbastecimentoCommand(int Id) : ICommand<bool>;
}
