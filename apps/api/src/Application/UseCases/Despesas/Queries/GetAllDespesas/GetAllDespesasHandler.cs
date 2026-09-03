using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Despesas.Queries.GetAllDespesas
{
    public sealed class GetAllDespesasHandler(IDespesaRepository repository,
                                              ICurrentUserService currentUser,
                                              ILogger<GetAllDespesasHandler> logger)
        : IQueryHandler<GetAllDespesasQuery, IEnumerable<DespesaResponse>>
    {
        public async Task<IEnumerable<DespesaResponse>> HandleAsync(GetAllDespesasQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando despesas | Veículo {VeiculoId} | Motorista {MotoristaId} | Tipo {TipoId} | De {De} | Até {Ate}",
                query.VeiculoId, query.MotoristaId, query.TipoDespesaId, query.De, query.Ate);

            if (query.De is not null && query.Ate is not null && query.Ate < query.De)
                throw new InvalidOperationException("A data final do período não pode ser anterior à inicial.");

            var despesas = await repository.GetAllAsync(
                currentUser.EmpresaId, query.VeiculoId, query.MotoristaId, query.TipoDespesaId, query.De, query.Ate);

            logger.LogInformation("Foram encontradas {Quantidade} despesas", despesas.Count());

            return despesas.Select(d => d.ToResponse());
        }
    }
}
