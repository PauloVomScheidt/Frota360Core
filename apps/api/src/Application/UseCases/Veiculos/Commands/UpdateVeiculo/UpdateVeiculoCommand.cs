using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.DTOs.Veiculo.Response;

namespace Frota360.Application.UseCases.Veiculos.Commands.UpdateVeiculo
{
    public sealed record UpdateVeiculoCommand(int Id, UpdateVeiculoRequest Data) : ICommand<VeiculoResponse?>;
}
