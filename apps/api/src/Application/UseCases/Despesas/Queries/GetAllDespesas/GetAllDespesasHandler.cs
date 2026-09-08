using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Request;
using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Despesas.Queries.GetAllDespesas
{
    public sealed class GetAllDespesasHandler(IDespesaRepository repository,
                                              ICurrentUserService currentUser,
                                              ILogger<GetAllDespesasHandler> logger)
        : IQueryHandler<GetAllDespesasQuery, ResultadoPaginado<DespesaResponse>>
    {
        public async Task<ResultadoPaginado<DespesaResponse>> HandleAsync(
            GetAllDespesasQuery query, CancellationToken cancellationToken = default)
        {
            var f = query.Filtro;

            logger.LogInformation("Buscando despesas | Página {Pagina} | Veículo {VeiculoId} | Motorista {MotoristaId} | Tipo {TipoId} | De {De} | Até {Ate}",
                f.Pagina, f.VeiculoId, f.MotoristaId, f.TipoDespesaId, f.De, f.Ate);

            if (f.De is not null && f.Ate is not null && f.Ate < f.De)
                throw new InvalidOperationException("A data final do período não pode ser anterior à inicial.");

            var (itens, total) = await repository.ConsultarAsync(currentUser.EmpresaId, Filtro(f));

            logger.LogInformation("Foram encontradas {Quantidade} despesas na página, {Total} no total",
                itens.Count(), total);

            return new ResultadoPaginado<DespesaResponse>
            {
                Itens = itens.Select(d => d.ToResponse()),
                Pagina = f.Pagina,
                TamanhoPagina = f.TamanhoPagina,
                Total = total
            };
        }

        /// <summary>Compartilhado com o handler do resumo para que lista e rodapé nunca divirjam.</summary>
        internal static FiltroDespesa Filtro(ConsultarDespesasRequest f)
            => new(f.Pagina, f.TamanhoPagina, f.VeiculoId, f.MotoristaId, f.TipoDespesaId, f.De, f.Ate);
    }
}
