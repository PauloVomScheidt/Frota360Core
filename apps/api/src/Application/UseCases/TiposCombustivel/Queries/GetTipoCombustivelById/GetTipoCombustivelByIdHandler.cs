using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoCombustivel.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposCombustivel.Queries.GetTipoCombustivelById
{
    public sealed class GetTipoCombustivelByIdHandler(ITipoCombustivelRepository repository,
                                                  ICurrentUserService currentUser,
                                                  ILogger<GetTipoCombustivelByIdHandler> logger)
        : IQueryHandler<GetTipoCombustivelByIdQuery, TipoCombustivelResponse?>
    {
        public async Task<TipoCombustivelResponse?> HandleAsync(GetTipoCombustivelByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando tipo de combustível Id {Id}", query.Id);

            var tipo = await repository.GetByIdAsync(query.Id, currentUser.EmpresaId);

            if (tipo is null)
            {
                logger.LogWarning("Tipo de combustível nao encontrado. Id {Id}", query.Id);
                return null;
            }

            return tipo.ToResponse();
        }
    }
}
