using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Manutencoes.Queries.GetAllManutencoes
{
    public sealed class GetAllManutencoesHandler(IManutencaoRepository repository, ICurrentUserService currentUser, ILogger<GetAllManutencoesHandler> logger)
        : IQueryHandler<GetAllManutencoesQuery, IEnumerable<ManutencaoResponse>>
    {
        public async Task<IEnumerable<ManutencaoResponse>> HandleAsync(GetAllManutencoesQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando manutenções | Veículo {VeiculoId} | Status {Status} | De {De} | Até {Ate}",
                query.VeiculoId, query.Status, query.De, query.Ate);

            // Intervalo invertido devolveria lista vazia sem explicar o porquê. Não há
            // validator neste GET (os filtros são query string solta), então a regra
            // segue o caminho de sempre: InvalidOperationException -> 422 com o texto.
            if (query.De is not null && query.Ate is not null && query.Ate < query.De)
                throw new InvalidOperationException("A data final do período não pode ser anterior à inicial.");

            var manutencoes = await repository.GetAllAsync(
                currentUser.EmpresaId, query.VeiculoId, query.Status, query.De, query.Ate);

            logger.LogInformation("Foram encontradas {QuantidadeManutencoes} manutenções", manutencoes.Count());

            return manutencoes.Select(m => m.ToResponse().SemCustoParaMotorista(currentUser));
        }
    }
}
