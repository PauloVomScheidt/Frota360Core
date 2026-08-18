using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Veiculos.Queries.GetVeiculoById
{
    public sealed class GetVeiculoByIdHandler(IVeiculoRepository repository, ILogger<GetVeiculoByIdHandler> logger)
        : IQueryHandler<GetVeiculoByIdQuery, VeiculoResponse?>
    {
        public async Task<VeiculoResponse?> HandleAsync(GetVeiculoByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando veículo Id {Id}", query.Id);

            var veiculo = await repository.GetByIdAsync(query.Id);

            if (veiculo is null)
            {
                logger.LogWarning("Veículo não encontrado. Id {Id}", query.Id);
                return null;
            }

            return veiculo.ToResponse();
        }
    }
}
