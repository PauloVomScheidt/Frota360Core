using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Custo.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Custos.Queries.GetCustos
{
    public sealed class GetCustosHandler(ICustoRepository repository,
                                         ICurrentUserService currentUser,
                                         ILogger<GetCustosHandler> logger)
        : IQueryHandler<GetCustosQuery, ResultadoPaginado<LancamentoCustoResponse>>
    {
        public async Task<ResultadoPaginado<LancamentoCustoResponse>> HandleAsync(
            GetCustosQuery query, CancellationToken cancellationToken = default)
        {
            var request = query.Data;

            logger.LogInformation("Consultando custos | Página {Pagina} | Veículo {VeiculoId} | Motorista {MotoristaId} | Origem {Origem} | De {De} | Até {Ate}",
                request.Pagina, request.VeiculoId, request.MotoristaId, request.Origem, request.De, request.Ate);

            try
            {
                var (itens, total) = await repository.ConsultarAsync(
                    currentUser.EmpresaId, request.ParaFiltro(), request.Pagina, request.TamanhoPagina);

                logger.LogInformation("Custos consultados. {Quantidade} lançamentos na página, {Total} no total",
                    itens.Count(), total);

                return new ResultadoPaginado<LancamentoCustoResponse>
                {
                    Itens = itens.Select(l => l.ToResponse()),
                    Pagina = request.Pagina,
                    TamanhoPagina = request.TamanhoPagina,
                    Total = total
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao consultar custos");
                throw;
            }
        }
    }
}
