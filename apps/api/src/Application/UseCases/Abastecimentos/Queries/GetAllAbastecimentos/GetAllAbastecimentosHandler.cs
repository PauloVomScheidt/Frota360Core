using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetAllAbastecimentos
{
    public sealed class GetAllAbastecimentosHandler(IAbastecimentoRepository repository,
                                                    ICurrentUserService currentUser,
                                                    ILogger<GetAllAbastecimentosHandler> logger)
        : IQueryHandler<GetAllAbastecimentosQuery, IEnumerable<AbastecimentoResponse>>
    {
        public async Task<IEnumerable<AbastecimentoResponse>> HandleAsync(GetAllAbastecimentosQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando abastecimentos | Veículo {VeiculoId} | Motorista {MotoristaId} | De {De} | Até {Ate}",
                query.VeiculoId, query.MotoristaId, query.De, query.Ate);

            if (query.De is not null && query.Ate is not null && query.Ate < query.De)
                throw new InvalidOperationException("A data final do período não pode ser anterior à inicial.");

            // Segundo eixo, do token e não do request: o motorista enxerga só o que é dele —
            // inclusive o que a gestão lançou para ele — e o filtro que vier no corpo é
            // sobrescrito. Para a gestão vale o que ela escolheu, ou a frota inteira.
            int? motoristaId = currentUser.EhMotorista() ? currentUser.UsuarioId : query.MotoristaId;

            var abastecimentos = await repository.GetAllAsync(
                currentUser.EmpresaId, query.VeiculoId, motoristaId, query.De, query.Ate);

            logger.LogInformation("Foram encontrados {Quantidade} abastecimentos", abastecimentos.Count());

            return abastecimentos.Select(a => a.ToResponse());
        }
    }
}
