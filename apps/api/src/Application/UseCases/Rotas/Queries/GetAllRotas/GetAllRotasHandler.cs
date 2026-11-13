using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Queries.GetAllRotas
{
    public sealed class GetAllRotasHandler(IRotaRepository repository,
                                           ICurrentUserService currentUser,
                                           ILogger<GetAllRotasHandler> logger)
        : IQueryHandler<GetAllRotasQuery, ResultadoPaginado<RotaResponse>>
    {
        public async Task<ResultadoPaginado<RotaResponse>> HandleAsync(
            GetAllRotasQuery query, CancellationToken cancellationToken = default)
        {
            var f = query.Filtro;

            logger.LogInformation("Buscando rotas | Página {Pagina} | Ativo {Ativo}", f.Pagina, f.Ativo);

            var (itens, total) = await repository.ConsultarAsync(
                currentUser.EmpresaId, new FiltroRota(f.Pagina, f.TamanhoPagina, f.Ativo));

            logger.LogInformation("Foram encontradas {Quantidade} rotas na página, {Total} no total",
                itens.Count(), total);

            return new ResultadoPaginado<RotaResponse>
            {
                Itens = itens.Select(r => r.ToResponse()),
                Pagina = f.Pagina,
                TamanhoPagina = f.TamanhoPagina,
                Total = total
            };
        }
    }
}
