using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoManutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposManutencao.Queries.GetAllTiposManutencao
{
    public sealed class GetAllTiposManutencaoHandler(ITipoManutencaoRepository repository, ICurrentUserService currentUser, ILogger<GetAllTiposManutencaoHandler> logger)
        : IQueryHandler<GetAllTiposManutencaoQuery, IEnumerable<TipoManutencaoResponse>>
    {
        public async Task<IEnumerable<TipoManutencaoResponse>> HandleAsync(GetAllTiposManutencaoQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando tipos de manutenção | ApenasAtivos {ApenasAtivos}", query.ApenasAtivos);

            var tipos = await repository.GetAllAsync(currentUser.EmpresaId, query.ApenasAtivos);

            logger.LogInformation("Foram encontrados {QuantidadeTipos} tipos de manutenção", tipos.Count());

            return tipos.Select(t => t.ToResponse());
        }
    }
}
