using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Queries.GetMinhasRotas
{
    /// <summary>
    /// Rotas do motorista logado. Escopo duplo — empresa (como todo o resto) e o
    /// próprio usuário, ambos vindos do token. O motorista é o usuário: não há id
    /// de motorista separado a resolver.
    /// </summary>
    public sealed class GetMinhasRotasHandler(IRotaRepository repository, ICurrentUserService currentUser, ILogger<GetMinhasRotasHandler> logger)
        : IQueryHandler<GetMinhasRotasQuery, IEnumerable<RotaResponse>>
    {
        public async Task<IEnumerable<RotaResponse>> HandleAsync(GetMinhasRotasQuery query, CancellationToken cancellationToken = default)
        {
            var motoristaId = currentUser.UsuarioId;

            logger.LogInformation("Buscando as rotas do motorista {MotoristaId}", motoristaId);

            var rotas = await repository.GetAllByMotoristaAsync(currentUser.EmpresaId, motoristaId);

            logger.LogInformation("Foram encontradas {QuantidadeRotas} rotas do motorista {MotoristaId}", rotas.Count(), motoristaId);

            return rotas.Select(r => r.ToResponse());
        }
    }
}
