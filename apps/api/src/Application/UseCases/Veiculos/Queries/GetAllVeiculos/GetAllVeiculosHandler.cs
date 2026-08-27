using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Veiculos;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Veiculos.Queries.GetAllVeiculos
{
    public sealed class GetAllVeiculosHandler(IVeiculoRepository repository, ICurrentUserService currentUser, ILogger<GetAllVeiculosHandler> logger)
        : IQueryHandler<GetAllVeiculosQuery, IEnumerable<VeiculoResponse>>
    {
        public async Task<IEnumerable<VeiculoResponse>> HandleAsync(GetAllVeiculosQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Buscando todos os veículos");

                var veiculos = await repository.GetAllAsync(currentUser.EmpresaId);

                logger.LogInformation("Foram encontrados {QuantidadeVeiculos} veículos", veiculos.Count());

                return veiculos.Select(v => v.ToResponse());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao buscar todos os veículos");
                throw;
            }
        }
    }
}
