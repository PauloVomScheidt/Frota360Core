using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Enums;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Manutencoes.Commands.UpdateManutencao
{
    public sealed class UpdateManutencaoHandler(IManutencaoRepository repository,
                                                IVeiculoRepository veiculoRepository,
                                                ITipoManutencaoRepository tipoRepository,
                                                ICurrentUserService currentUser,
                                                ILogger<UpdateManutencaoHandler> logger)
        : ICommandHandler<UpdateManutencaoCommand, ManutencaoResponse?>
    {
        public async Task<ManutencaoResponse?> HandleAsync(UpdateManutencaoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização da manutenção Id {Id}", command.Id);

                var manutencao = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (manutencao is null)
                {
                    logger.LogWarning("Tentativa de atualizar manutenção inexistente. Id {Id}", command.Id);
                    return null;
                }

                // Replanejar só faz sentido enquanto está pendente: o que já foi executado
                // vira histórico e não deve ser reescrito por este endpoint.
                if (manutencao.Status != StatusManutencao.Pendente)
                    throw new InvalidOperationException(
                        $"Manutenção {command.Id} está {manutencao.Status.ToString().ToLowerInvariant()} e não pode ser alterada.");

                var request = command.Data;

                var veiculo = await veiculoRepository.GetByIdAsync(request.VeiculoId, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException($"Veículo {request.VeiculoId} não encontrado.");

                var tipo = await tipoRepository.GetByIdAsync(request.TipoManutencaoId, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException($"Tipo de manutenção {request.TipoManutencaoId} não encontrado.");

                if (await repository.ExisteDuplicadaAsync(currentUser.EmpresaId, request.VeiculoId,
                        request.TipoManutencaoId, request.QuilometragemPrevista, ignorarId: manutencao.Id))
                    throw new InvalidOperationException(
                        $"Já existe manutenção pendente de \"{tipo.Nome}\" para este veículo em {request.QuilometragemPrevista} km.");

                manutencao.VeiculoId = veiculo.Id;
                manutencao.TipoManutencaoId = tipo.Id;
                manutencao.QuilometragemPrevista = request.QuilometragemPrevista;
                manutencao.DataPrevista = request.DataPrevista;
                manutencao.Observacao = request.Observacao;

                var atualizada = await repository.UpdateAsync(manutencao);
                atualizada.Veiculo = veiculo;
                atualizada.Tipo = tipo;

                logger.LogInformation("Manutenção atualizada com sucesso. Id {Id}", atualizada.Id);

                return atualizada.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao atualizar manutenção Id {Id}", command.Id);
                throw;
            }
        }
    }
}
