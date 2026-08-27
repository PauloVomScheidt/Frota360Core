using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.Manutencoes.Commands.DeleteManutencao
{
    public sealed record DeleteManutencaoCommand(int Id) : ICommand<bool>;
}
