using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposCombustivel.Commands.DeleteTipoCombustivel
{
    public sealed class DeleteTipoCombustivelHandler(ITipoCombustivelRepository repository,
                                                     IAbastecimentoRepository abastecimentoRepository,
                                                     ICurrentUserService currentUser,
                                                     IAuditoriaService auditoria,
                                                     ILogger<DeleteTipoCombustivelHandler> logger)
        : ICommandHandler<DeleteTipoCombustivelCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteTipoCombustivelCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do tipo de combustível Id {Id}", command.Id);

                var tipo = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (tipo is null)
                {
                    logger.LogWarning("Tentativa de remover tipo de combustível inexistente. Id {Id}", command.Id);
                    return false;
                }

                // Tipo em uso não some: apagá-lo levaria junto o histórico de abastecimento.
                // Para tirá-lo de circulação sem perder o passado, basta marcá-lo como inativo.
                if (await abastecimentoRepository.ExisteComTipoCombustivelAsync(currentUser.EmpresaId, tipo.Id))
                    throw new InvalidOperationException(
                        $"O combustível \"{tipo.Nome}\" está em uso por abastecimentos e não pode ser excluído. Inative-o.");

                await repository.DeleteAsync(tipo);

                logger.LogInformation("Tipo de combustível removido com sucesso. Id {Id}", command.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.TipoCombustivel, AcoesAuditoria.Excluiu, command.Id,
                    $"Excluiu o tipo de combustível \"{tipo.Nome}\"");

                return true;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao remover tipo de combustível Id {Id}", command.Id);
                throw;
            }
        }
    }
}
