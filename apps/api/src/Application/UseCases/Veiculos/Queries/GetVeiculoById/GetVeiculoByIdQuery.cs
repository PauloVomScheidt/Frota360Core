using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Response;

namespace Frota360.Application.UseCases.Veiculos.Queries.GetVeiculoById
{
    public sealed record GetVeiculoByIdQuery(int Id) : IQuery<VeiculoResponse?>;
}
