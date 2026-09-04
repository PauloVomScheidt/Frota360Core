using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Posto.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Postos.Queries.GetAllPostos
{
    public sealed class GetAllPostosHandler(IPostoRepository repository,
                                            ICurrentUserService currentUser,
                                            ILogger<GetAllPostosHandler> logger)
        : IQueryHandler<GetAllPostosQuery, IEnumerable<PostoResponse>>
    {
        public async Task<IEnumerable<PostoResponse>> HandleAsync(GetAllPostosQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando postos | Apenas ativos {ApenasAtivos}", query.ApenasAtivos);

            var postos = await repository.GetAllAsync(currentUser.EmpresaId, query.ApenasAtivos);

            logger.LogInformation("Foram encontrados {Quantidade} postos", postos.Count());

            return postos.Select(p => p.ToResponse());
        }
    }
}
