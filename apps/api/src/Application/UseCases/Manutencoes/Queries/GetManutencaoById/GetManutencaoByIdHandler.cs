using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Manutencoes.Queries.GetManutencaoById
{
    public sealed class GetManutencaoByIdHandler(IManutencaoRepository repository, ICurrentUserService currentUser, ILogger<GetManutencaoByIdHandler> logger)
        : IQueryHandler<GetManutencaoByIdQuery, ManutencaoResponse?>
    {
        public async Task<ManutencaoResponse?> HandleAsync(GetManutencaoByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando manutenção Id {Id}", query.Id);

            var manutencao = await repository.GetByIdAsync(query.Id, currentUser.EmpresaId);

            if (manutencao is null)
            {
                logger.LogWarning("Manutenção não encontrada. Id {Id}", query.Id);
                return null;
            }

            return manutencao.ToResponse();
        }
    }
}
