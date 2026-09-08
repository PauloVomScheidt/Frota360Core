using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoCombustivel.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposCombustivel.Queries.GetAllTiposCombustivel
{
    public sealed class GetAllTiposCombustivelHandler(ITipoCombustivelRepository repository,
                                                  ICurrentUserService currentUser,
                                                  ILogger<GetAllTiposCombustivelHandler> logger)
        : IQueryHandler<GetAllTiposCombustivelQuery, IEnumerable<TipoCombustivelResponse>>
    {
        public async Task<IEnumerable<TipoCombustivelResponse>> HandleAsync(GetAllTiposCombustivelQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando tipos de combustível | Apenas ativos {ApenasAtivos}", query.ApenasAtivos);

            var tipos = await repository.GetAllAsync(currentUser.EmpresaId, query.ApenasAtivos);

            logger.LogInformation("Foram encontrados {Quantidade} tipos de combustível", tipos.Count());

            return tipos.Select(t => t.ToResponse());
        }
    }
}
