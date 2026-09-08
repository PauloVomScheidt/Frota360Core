using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Queries.GetResumoRotas
{
    public sealed class GetResumoRotasHandler(IRotaRepository repository,
                                              ICurrentUserService currentUser,
                                              ILogger<GetResumoRotasHandler> logger)
        : IQueryHandler<GetResumoRotasQuery, ResumoRotasResponse>
    {
        public async Task<ResumoRotasResponse> HandleAsync(
            GetResumoRotasQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Resumindo rotas encerradas | De {De} | Até {Ate}", query.De, query.Ate);

            if (query.Ate < query.De)
                throw new InvalidOperationException("A data final do período não pode ser anterior à inicial.");

            var resumo = await repository.ResumirEncerradasAsync(currentUser.EmpresaId, query.De, query.Ate);

            logger.LogInformation("Resumo: {Quantidade} rotas encerradas somando {KmTotal} km",
                resumo.Quantidade, resumo.KmTotal);

            return new ResumoRotasResponse { Quantidade = resumo.Quantidade, KmTotal = resumo.KmTotal };
        }
    }
}
