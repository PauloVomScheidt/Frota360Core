using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Veiculos.Queries.GetVeiculoById
{
    public sealed class GetVeiculoByIdHandler(IVeiculoRepository repository,
                                              IRotaRepository rotaRepository,
                                              ICurrentUserService currentUser,
                                              ILogger<GetVeiculoByIdHandler> logger)
        : IQueryHandler<GetVeiculoByIdQuery, VeiculoResponse?>
    {
        public async Task<VeiculoResponse?> HandleAsync(GetVeiculoByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando veículo Id {Id}", query.Id);

            var veiculo = await repository.GetByIdAsync(query.Id, currentUser.EmpresaId);

            if (veiculo is null)
            {
                logger.LogWarning("Veículo não encontrado. Id {Id}", query.Id);
                return null;
            }

            var emRota = await rotaRepository.ExisteRotaAtivaComVeiculoAsync(currentUser.EmpresaId, veiculo.Id);

            return veiculo.ToResponse(emRota);
        }
    }
}
