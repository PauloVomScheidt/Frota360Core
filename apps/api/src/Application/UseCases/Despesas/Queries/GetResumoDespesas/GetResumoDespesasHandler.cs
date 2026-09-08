using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Despesas.Queries.GetAllDespesas;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Despesas.Queries.GetResumoDespesas
{
    public sealed class GetResumoDespesasHandler(IDespesaRepository repository,
                                                 ICurrentUserService currentUser,
                                                 ILogger<GetResumoDespesasHandler> logger)
        : IQueryHandler<GetResumoDespesasQuery, ResumoDespesasResponse>
    {
        public async Task<ResumoDespesasResponse> HandleAsync(
            GetResumoDespesasQuery query, CancellationToken cancellationToken = default)
        {
            var f = query.Filtro;

            logger.LogInformation("Resumindo despesas | Veículo {VeiculoId} | Tipo {TipoId} | De {De} | Até {Ate}",
                f.VeiculoId, f.TipoDespesaId, f.De, f.Ate);

            if (f.De is not null && f.Ate is not null && f.Ate < f.De)
                throw new InvalidOperationException("A data final do período não pode ser anterior à inicial.");

            var resumo = await repository.ResumirAsync(currentUser.EmpresaId, GetAllDespesasHandler.Filtro(f));

            logger.LogInformation("Resumo: {Quantidade} despesas somando {ValorTotal}",
                resumo.Quantidade, resumo.ValorTotal);

            return new ResumoDespesasResponse
            {
                Quantidade = resumo.Quantidade,
                ValorTotal = resumo.ValorTotal
            };
        }
    }
}
