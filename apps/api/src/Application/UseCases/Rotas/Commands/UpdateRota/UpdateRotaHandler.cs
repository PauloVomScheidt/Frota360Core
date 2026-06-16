using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.UseCases.Rotas;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Commands.UpdateRota
{
    public sealed class UpdateRotaHandler(IRotaRepository repository, ILogger<UpdateRotaHandler> logger)
        : ICommandHandler<UpdateRotaCommand, RotaResponse?>
    {
        public async Task<RotaResponse?> HandleAsync(UpdateRotaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização da rota Id {Id}", command.Id);

                var rota = await repository.GetByIdAsync(command.Id);

                if (rota is null)
                {
                    logger.LogWarning("Tentativa de atualizar rota inexistente. Id {Id}", command.Id);
                    return null;
                }

                var request = command.Data;
                rota.Origem = request.Origem;
                rota.Destino = request.Destino;
                rota.CodigoMotorista = request.CodigoMotorista;
                rota.CodigoVeiculo = request.CodigoVeiculo;
                rota.Ativo = request.Ativo;
                rota.DataInicio = request.DataInicio;
                rota.DataFim = request.DataFim;

                var atualizado = await repository.UpdateAsync(rota);

                logger.LogInformation("Rota atualizada com sucesso. Id {Id}", atualizado.Id);

                return atualizado.ToResponse();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao atualizar rota Id {Id}", command.Id);
                throw;
            }
        }
    }
}
