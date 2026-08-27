using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Manutencoes.Commands.DeleteManutencao
{
    public sealed class DeleteManutencaoHandler(IManutencaoRepository repository, ICurrentUserService currentUser, ILogger<DeleteManutencaoHandler> logger)
        : ICommandHandler<DeleteManutencaoCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteManutencaoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção da manutenção Id {Id}", command.Id);

                var manutencao = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (manutencao is null)
                {
                    logger.LogWarning("Tentativa de remover manutenção inexistente. Id {Id}", command.Id);
                    return false;
                }

                await repository.DeleteAsync(manutencao);

                logger.LogInformation("Manutenção removida com sucesso. Id {Id}", command.Id);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover manutenção Id {Id}", command.Id);
                throw;
            }
        }
    }
}
