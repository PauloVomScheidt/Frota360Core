using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposManutencao.Commands.DeleteTipoManutencao
{
    public sealed class DeleteTipoManutencaoHandler(ITipoManutencaoRepository repository,
                                                    IManutencaoRepository manutencaoRepository,
                                                    ICurrentUserService currentUser,
                                                    ILogger<DeleteTipoManutencaoHandler> logger)
        : ICommandHandler<DeleteTipoManutencaoCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteTipoManutencaoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do tipo de manutenção Id {Id}", command.Id);

                var tipo = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (tipo is null)
                {
                    logger.LogWarning("Tentativa de remover tipo de manutenção inexistente. Id {Id}", command.Id);
                    return false;
                }

                // Tipo em uso não some: apagá-lo levaria junto o histórico do veículo.
                // Para tirá-lo de circulação sem perder o passado, basta marcá-lo como inativo.
                if (await manutencaoRepository.ExisteComTipoAsync(currentUser.EmpresaId, tipo.Id))
                    throw new InvalidOperationException(
                        $"O tipo \"{tipo.Nome}\" está em uso por manutenções e não pode ser excluído. Inative-o.");

                await repository.DeleteAsync(tipo);

                logger.LogInformation("Tipo de manutenção removido com sucesso. Id {Id}", command.Id);

                return true;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao remover tipo de manutenção Id {Id}", command.Id);
                throw;
            }
        }
    }
}
