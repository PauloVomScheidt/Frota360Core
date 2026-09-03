using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoDespesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposDespesa.Queries.GetTipoDespesaById
{
    public sealed class GetTipoDespesaByIdHandler(ITipoDespesaRepository repository,
                                                  ICurrentUserService currentUser,
                                                  ILogger<GetTipoDespesaByIdHandler> logger)
        : IQueryHandler<GetTipoDespesaByIdQuery, TipoDespesaResponse?>
    {
        public async Task<TipoDespesaResponse?> HandleAsync(GetTipoDespesaByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando tipo de despesa Id {Id}", query.Id);

            var tipo = await repository.GetByIdAsync(query.Id, currentUser.EmpresaId);

            if (tipo is null)
            {
                logger.LogWarning("Tipo de despesa nao encontrado. Id {Id}", query.Id);
                return null;
            }

            return tipo.ToResponse();
        }
    }
}
