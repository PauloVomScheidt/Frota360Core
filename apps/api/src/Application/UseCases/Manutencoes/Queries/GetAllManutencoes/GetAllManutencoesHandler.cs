using Frota360.Application.Abstractions.Messaging;
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
            logger.LogInformation("Buscando manutenções | Veículo {VeiculoId} | Status {Status}", query.VeiculoId, query.Status);

            var manutencoes = await repository.GetAllAsync(currentUser.EmpresaId, query.VeiculoId, query.Status);

            logger.LogInformation("Foram encontradas {QuantidadeManutencoes} manutenções", manutencoes.Count());

            return manutencoes.Select(m => m.ToResponse());
        }
    }
}
