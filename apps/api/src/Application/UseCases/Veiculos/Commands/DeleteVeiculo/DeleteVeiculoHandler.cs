using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Veiculos.Commands.DeleteVeiculo
{
    public sealed class DeleteVeiculoHandler(IVeiculoRepository repository, ICurrentUserService currentUser, ILogger<DeleteVeiculoHandler> logger)
        : ICommandHandler<DeleteVeiculoCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteVeiculoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do veículo Id {Id}", command.Id);

                var veiculo = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (veiculo is null)
                {
                    logger.LogWarning("Tentativa de remover veículo inexistente. Id {Id}", command.Id);
                    return false;
                }

                await repository.DeleteAsync(veiculo);

                logger.LogInformation("Veículo removido com sucesso. Id {Id}", command.Id);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover veículo Id {Id}", command.Id);
                throw;
            }
        }
    }
}
