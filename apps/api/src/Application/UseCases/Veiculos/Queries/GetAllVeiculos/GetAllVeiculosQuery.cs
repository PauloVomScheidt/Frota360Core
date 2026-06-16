using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Response;

namespace Frota360.Application.UseCases.Veiculos.Queries.GetAllVeiculos
{
    public sealed record GetAllVeiculosQuery : IQuery<IEnumerable<VeiculoResponse>>;
}
