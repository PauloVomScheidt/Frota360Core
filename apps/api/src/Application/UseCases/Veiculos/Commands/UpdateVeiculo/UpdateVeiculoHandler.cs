using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Veiculos;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Veiculos.Commands.UpdateVeiculo
{
    public sealed class UpdateVeiculoHandler(IVeiculoRepository repository,
                                             ICurrentUserService currentUser,
                                             IAuditoriaService auditoria,
                                             ILogger<UpdateVeiculoHandler> logger)
        : ICommandHandler<UpdateVeiculoCommand, VeiculoResponse?>
    {
        public async Task<VeiculoResponse?> HandleAsync(UpdateVeiculoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização do veículo Id {Id}", command.Id);

                var veiculo = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (veiculo is null)
                {
                    logger.LogWarning("Tentativa de atualizar veículo inexistente. Id {Id}", command.Id);
                    return null;
                }

                var request = command.Data;

                // O diff é montado antes de a entidade ser mutada — depois disso o "antes" se perde.
                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Nome", veiculo.NomeVeiculo, request.NomeVeiculo)
                    .Comparar("Marca", veiculo.MarcaVeiculo, request.MarcaVeiculo)
                    .Comparar("Placa", veiculo.Placa, request.Placa)
                    .Comparar("Quilometragem", veiculo.Quilometragem, request.Quilometragem)
                    .Comparar("Último motorista", veiculo.UltimoMotorista, request.UltimoMotorista)
                    .Comparar("Data da última viagem", veiculo.DataUltimaViagem, request.DataUltimaViagem)
                    .Construir();

                veiculo.NomeVeiculo = request.NomeVeiculo;
                veiculo.MarcaVeiculo = request.MarcaVeiculo;
                veiculo.Placa = request.Placa;
                veiculo.Quilometragem = request.Quilometragem;
                veiculo.UltimoMotorista = request.UltimoMotorista;
                veiculo.DataUltimaViagem = request.DataUltimaViagem;

                var atualizado = await repository.UpdateAsync(veiculo);

                logger.LogInformation("Veículo atualizado com sucesso. Id {Id}", atualizado.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Veiculo, AcoesAuditoria.Atualizou, atualizado.Id,
                    $"Atualizou o veículo {atualizado.Placa}", alteracoes);

                return atualizado.ToResponse();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao atualizar veículo Id {Id}", command.Id);
                throw;
            }
        }
    }
}
