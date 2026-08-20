using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Motoristas.Queries.GetMotoristaById
{
    public sealed class GetMotoristaByIdHandler(IMotoristaRepository repository, ILogger<GetMotoristaByIdHandler> logger)
        : IQueryHandler<GetMotoristaByIdQuery, MotoristaResponse?>
    {
        public async Task<MotoristaResponse?> HandleAsync(GetMotoristaByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando motorista Id {Id}", query.Id);

            var motorista = await repository.GetByIdAsync(query.Id);

            if (motorista is null)
            {
                logger.LogWarning("Motorista não encontrado. Id {Id}", query.Id);
                return null;
            }

            return motorista.ToResponse();
        }
    }
}
