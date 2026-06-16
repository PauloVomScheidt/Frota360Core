using Frota360.Application.Abstractions.Messaging;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Commands.DeleteRota
{
    public sealed class DeleteRotaHandler(IRotaRepository repository, ILogger<DeleteRotaHandler> logger)
        : ICommandHandler<DeleteRotaCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteRotaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção da rota Id {Id}", command.Id);

                var rota = await repository.GetByIdAsync(command.Id);

                if (rota is null)
                {
                    logger.LogWarning("Tentativa de remover rota inexistente. Id {Id}", command.Id);
                    return false;
                }

                await repository.DeleteAsync(rota);

                logger.LogInformation("Rota removida com sucesso. Id {Id}", command.Id);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover rota Id {Id}", command.Id);
                throw;
            }
        }
    }
}
