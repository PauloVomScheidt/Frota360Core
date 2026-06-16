using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.UseCases.Rotas;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Queries.GetAllRotas
{
    public sealed class GetAllRotasHandler(IRotaRepository repository, ILogger<GetAllRotasHandler> logger)
        : IQueryHandler<GetAllRotasQuery, IEnumerable<RotaResponse>>
    {
        public async Task<IEnumerable<RotaResponse>> HandleAsync(GetAllRotasQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando todas as rotas");

            var rotas = await repository.GetAllAsync();

            logger.LogInformation("Foram encontradas {QuantidadeRotas} rotas", rotas.Count());

            return rotas.Select(r => r.ToResponse());
        }
    }
}
