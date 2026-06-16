using Frota360.Application.Abstractions.Messaging;

namespace Frota360.Application.UseCases.Veiculos.Commands.DeleteVeiculo
{
    public sealed record DeleteVeiculoCommand(int Id) : ICommand<bool>;
}
