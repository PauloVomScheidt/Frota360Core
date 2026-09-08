using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Posto.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Postos.Queries.GetPostoById
{
    public sealed class GetPostoByIdHandler(IPostoRepository repository,
                                            ICurrentUserService currentUser,
                                            ILogger<GetPostoByIdHandler> logger)
        : IQueryHandler<GetPostoByIdQuery, PostoResponse?>
    {
        public async Task<PostoResponse?> HandleAsync(GetPostoByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando posto Id {Id}", query.Id);

            var posto = await repository.GetByIdAsync(query.Id, currentUser.EmpresaId);

            if (posto is null)
            {
                logger.LogWarning("Posto nao encontrado. Id {Id}", query.Id);
                return null;
            }

            return posto.ToResponse();
        }
    }
}
