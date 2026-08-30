using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Abastecimentos.Commands.DeleteAbastecimento
{
    public sealed class DeleteAbastecimentoHandler(IAbastecimentoRepository repository,
                                                   ICurrentUserService currentUser,
                                                   IAuditoriaService auditoria,
                                                   ILogger<DeleteAbastecimentoHandler> logger)
        : ICommandHandler<DeleteAbastecimentoCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteAbastecimentoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do abastecimento Id {Id}", command.Id);

                var abastecimento = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (abastecimento is null)
                {
                    logger.LogWarning("Tentativa de remover abastecimento inexistente. Id {Id}", command.Id);
                    return false;
                }

                await repository.DeleteAsync(abastecimento);

                logger.LogInformation("Abastecimento removido com sucesso. Id {Id}", command.Id);

                // A descrição carrega os números porque o registro deixou de existir: é o
                // único lugar onde o gasto excluído continua legível.
                await auditoria.RegistrarAsync(EntidadesAuditadas.Abastecimento, AcoesAuditoria.Excluiu, command.Id,
                    $"Excluiu o abastecimento #{command.Id} de R$ {abastecimento.Valor:0.00} " +
                    $"no veículo {abastecimento.Veiculo?.Placa} de {abastecimento.Motorista?.Nome}");

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover abastecimento Id {Id}", command.Id);
                throw;
            }
        }
    }
}
