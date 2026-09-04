using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Postos.Commands.DeletePosto
{
    public sealed class DeletePostoHandler(IPostoRepository repository,
                                           IAbastecimentoRepository abastecimentoRepository,
                                           ICurrentUserService currentUser,
                                           IAuditoriaService auditoria,
                                           ILogger<DeletePostoHandler> logger)
        : ICommandHandler<DeletePostoCommand, bool>
    {
        public async Task<bool> HandleAsync(DeletePostoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do posto Id {Id}", command.Id);

                var posto = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (posto is null)
                {
                    logger.LogWarning("Tentativa de remover posto inexistente. Id {Id}", command.Id);
                    return false;
                }

                // Posto em uso não some: apagá-lo levaria junto o histórico de abastecimento.
                // Descredenciar é inativar — o passado continua nomeado.
                if (await abastecimentoRepository.ExisteComPostoAsync(currentUser.EmpresaId, posto.Id))
                    throw new InvalidOperationException(
                        $"O posto \"{posto.Nome}\" está em uso por abastecimentos e não pode ser excluído. Inative-o.");

                await repository.DeleteAsync(posto);

                logger.LogInformation("Posto removido com sucesso. Id {Id}", command.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Posto, AcoesAuditoria.Excluiu, command.Id,
                    $"Excluiu o posto \"{posto.Nome}\"");

                return true;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao remover posto Id {Id}", command.Id);
                throw;
            }
        }
    }
}
