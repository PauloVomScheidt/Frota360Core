using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposDespesa.Commands.DeleteTipoDespesa
{
    public sealed class DeleteTipoDespesaHandler(ITipoDespesaRepository repository,
                                                 IDespesaRepository despesaRepository,
                                                 ICurrentUserService currentUser,
                                                 IAuditoriaService auditoria,
                                                 ILogger<DeleteTipoDespesaHandler> logger)
        : ICommandHandler<DeleteTipoDespesaCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteTipoDespesaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do tipo de despesa Id {Id}", command.Id);

                var tipo = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (tipo is null)
                {
                    logger.LogWarning("Tentativa de remover tipo de despesa inexistente. Id {Id}", command.Id);
                    return false;
                }

                // Tipo em uso não some: apagá-lo levaria junto o histórico financeiro.
                // Para tirá-lo de circulação sem perder o passado, basta marcá-lo como inativo.
                if (await despesaRepository.ExisteComTipoAsync(currentUser.EmpresaId, tipo.Id))
                    throw new InvalidOperationException(
                        $"O tipo \"{tipo.Nome}\" está em uso por despesas e não pode ser excluído. Inative-o.");

                await repository.DeleteAsync(tipo);

                logger.LogInformation("Tipo de despesa removido com sucesso. Id {Id}", command.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.TipoDespesa, AcoesAuditoria.Excluiu, command.Id,
                    $"Excluiu o tipo de despesa \"{tipo.Nome}\"");

                return true;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao remover tipo de despesa Id {Id}", command.Id);
                throw;
            }
        }
    }
}
