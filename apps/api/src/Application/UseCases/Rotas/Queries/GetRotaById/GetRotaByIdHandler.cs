using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Queries.GetRotaById
{
    public sealed class GetRotaByIdHandler(IRotaRepository repository, ICurrentUserService currentUser, ILogger<GetRotaByIdHandler> logger)
        : IQueryHandler<GetRotaByIdQuery, RotaResponse?>
    {
        public async Task<RotaResponse?> HandleAsync(GetRotaByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando rota Id {Id}", query.Id);

            var rota = await repository.GetByIdAsync(query.Id, currentUser.EmpresaId);

            if (rota is null)
            {
                logger.LogWarning("Rota não encontrada. Id {Id}", query.Id);
                return null;
            }

            return rota.ToResponse();
        }
    }
}
