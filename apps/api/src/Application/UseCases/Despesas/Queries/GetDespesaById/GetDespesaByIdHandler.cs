using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Despesas.Queries.GetDespesaById
{
    public sealed class GetDespesaByIdHandler(IDespesaRepository repository,
                                              ICurrentUserService currentUser,
                                              ILogger<GetDespesaByIdHandler> logger)
        : IQueryHandler<GetDespesaByIdQuery, DespesaResponse?>
    {
        public async Task<DespesaResponse?> HandleAsync(GetDespesaByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando despesa Id {Id}", query.Id);

            var despesa = await repository.GetByIdAsync(query.Id, currentUser.EmpresaId);

            if (despesa is null)
            {
                logger.LogWarning("Despesa não encontrada. Id {Id}", query.Id);
                return null;
            }

            return despesa.ToResponse();
        }
    }
}
