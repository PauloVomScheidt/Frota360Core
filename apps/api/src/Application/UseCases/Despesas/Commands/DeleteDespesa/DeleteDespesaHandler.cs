using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Despesas.Commands.DeleteDespesa
{
    public sealed class DeleteDespesaHandler(IDespesaRepository repository,
                                             ICurrentUserService currentUser,
                                             IAuditoriaService auditoria,
                                             ILogger<DeleteDespesaHandler> logger)
        : ICommandHandler<DeleteDespesaCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteDespesaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção da despesa Id {Id}", command.Id);

                var despesa = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (despesa is null)
                {
                    logger.LogWarning("Tentativa de remover despesa inexistente. Id {Id}", command.Id);
                    return false;
                }

                await repository.DeleteAsync(despesa);

                logger.LogInformation("Despesa removida com sucesso. Id {Id}", command.Id);

                // A descrição carrega os números porque o registro deixou de existir: é o
                // único lugar onde o gasto excluído continua legível.
                await auditoria.RegistrarAsync(EntidadesAuditadas.Despesa, AcoesAuditoria.Excluiu, command.Id,
                    $"Excluiu a despesa #{command.Id} de R$ {despesa.Valor:0.00} " +
                    $"({despesa.Tipo?.Nome}) no veículo {despesa.Veiculo?.Placa}");

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover despesa Id {Id}", command.Id);
                throw;
            }
        }
    }
}
