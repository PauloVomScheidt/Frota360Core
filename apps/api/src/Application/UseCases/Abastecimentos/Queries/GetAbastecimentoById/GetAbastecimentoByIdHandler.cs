using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetAbastecimentoById
{
    public sealed class GetAbastecimentoByIdHandler(IAbastecimentoRepository repository,
                                                    ICurrentUserService currentUser,
                                                    ILogger<GetAbastecimentoByIdHandler> logger)
        : IQueryHandler<GetAbastecimentoByIdQuery, AbastecimentoResponse?>
    {
        public async Task<AbastecimentoResponse?> HandleAsync(GetAbastecimentoByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando abastecimento Id {Id}", query.Id);

            var abastecimento = await repository.GetByIdAsync(query.Id, currentUser.EmpresaId);

            if (abastecimento is null)
                return null;

            // Lançamento de outro motorista "não existe" para ele — 404, não 403, igual à
            // rota alheia.
            if (currentUser.EhMotorista() && abastecimento.MotoristaId != currentUser.UsuarioId)
            {
                logger.LogWarning("Motorista {UsuarioId} tentou ler o abastecimento {Id}, que não é dele",
                    currentUser.UsuarioId, query.Id);
                return null;
            }

            return abastecimento.ToResponse();
        }
    }
}
