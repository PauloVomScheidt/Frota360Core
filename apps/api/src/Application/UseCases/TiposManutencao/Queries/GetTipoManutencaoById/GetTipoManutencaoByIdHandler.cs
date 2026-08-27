using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoManutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposManutencao.Queries.GetTipoManutencaoById
{
    public sealed class GetTipoManutencaoByIdHandler(ITipoManutencaoRepository repository, ICurrentUserService currentUser, ILogger<GetTipoManutencaoByIdHandler> logger)
        : IQueryHandler<GetTipoManutencaoByIdQuery, TipoManutencaoResponse?>
    {
        public async Task<TipoManutencaoResponse?> HandleAsync(GetTipoManutencaoByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando tipo de manutenção Id {Id}", query.Id);

            var tipo = await repository.GetByIdAsync(query.Id, currentUser.EmpresaId);

            if (tipo is null)
            {
                logger.LogWarning("Tipo de manutenção não encontrado. Id {Id}", query.Id);
                return null;
            }

            return tipo.ToResponse();
        }
    }
}
