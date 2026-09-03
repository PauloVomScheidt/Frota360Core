using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoDespesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposDespesa.Queries.GetAllTiposDespesa
{
    public sealed class GetAllTiposDespesaHandler(ITipoDespesaRepository repository,
                                                  ICurrentUserService currentUser,
                                                  ILogger<GetAllTiposDespesaHandler> logger)
        : IQueryHandler<GetAllTiposDespesaQuery, IEnumerable<TipoDespesaResponse>>
    {
        public async Task<IEnumerable<TipoDespesaResponse>> HandleAsync(GetAllTiposDespesaQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando tipos de despesa | Apenas ativos {ApenasAtivos}", query.ApenasAtivos);

            var tipos = await repository.GetAllAsync(currentUser.EmpresaId, query.ApenasAtivos);

            logger.LogInformation("Foram encontrados {Quantidade} tipos de despesa", tipos.Count());

            return tipos.Select(t => t.ToResponse());
        }
    }
}
