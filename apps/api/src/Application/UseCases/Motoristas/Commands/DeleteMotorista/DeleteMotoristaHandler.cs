using Frota360.Application.Abstractions.Messaging;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Motoristas.Commands.DeleteMotorista
{
    public sealed class DeleteMotoristaHandler(IMotoristaRepository repository, ILogger<DeleteMotoristaHandler> logger)
        : ICommandHandler<DeleteMotoristaCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteMotoristaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do motorista Id {Id}", command.Id);

                var motorista = await repository.GetByIdAsync(command.Id);

                if (motorista is null)
                {
                    logger.LogWarning("Tentativa de remover motorista inexistente. Id {Id}", command.Id);
                    return false;
                }

                await repository.DeleteAsync(motorista);

                logger.LogInformation("Motorista removido com sucesso. Id {Id}", command.Id);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover motorista Id {Id}", command.Id);
                throw;
            }
        }
    }
}
