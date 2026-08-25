using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Motoristas;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Motoristas.Commands.UpdateMotorista
{
    public sealed class UpdateMotoristaHandler(IMotoristaRepository repository, ICurrentUserService currentUser, ILogger<UpdateMotoristaHandler> logger)
        : ICommandHandler<UpdateMotoristaCommand, MotoristaResponse?>
    {
        public async Task<MotoristaResponse?> HandleAsync(UpdateMotoristaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização do motorista Id {Id}", command.Id);

                var motorista = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (motorista is null)
                {
                    logger.LogWarning("Tentativa de atualizar motorista inexistente. Id {Id}", command.Id);
                    return null;
                }

                var request = command.Data;
                motorista.Nome = request.Nome;
                motorista.Email = request.Email;
                motorista.CPF = request.CPF;
                motorista.DataNascimento = request.DataNascimento;

                var atualizado = await repository.UpdateAsync(motorista);

                logger.LogInformation("Motorista atualizado com sucesso. Id {Id}", atualizado.Id);

                return atualizado.ToResponse();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao atualizar motorista Id {Id}", command.Id);
                throw;
            }
        }
    }
}
