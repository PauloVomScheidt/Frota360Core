using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Rotas;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Commands.EncerrarRota
{
    /// <summary>
    /// Fecha o ciclo da rota: grava o hodômetro final, calcula a quilometragem percorrida
    /// e aproveita o número para manter o veículo atualizado — é o momento em que alguém
    /// olhou o odômetro de verdade.
    /// </summary>
    public sealed class EncerrarRotaHandler(IRotaRepository repository,
                                            IVeiculoRepository veiculoRepository,
                                            ICurrentUserService currentUser,
                                            ILogger<EncerrarRotaHandler> logger)
        : ICommandHandler<EncerrarRotaCommand, RotaResponse?>
    {
        public async Task<RotaResponse?> HandleAsync(EncerrarRotaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando encerramento da rota Id {Id}", command.Id);

                var rota = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (rota is null)
                {
                    logger.LogWarning("Tentativa de encerrar rota inexistente. Id {Id}", command.Id);
                    return null;
                }

                if (rota.DataFim is not null)
                    throw new InvalidOperationException("Esta rota já foi encerrada.");

                var request = command.Data;

                if (request.KmFinal < rota.KmInicial)
                    throw new InvalidOperationException("A quilometragem final não pode ser menor que a inicial.");

                var dataFim = request.DataFim ?? DateTime.UtcNow;

                if (dataFim < rota.DataInicio)
                    throw new InvalidOperationException("A data de fim não pode ser anterior à data de início.");

                rota.KmFinal = request.KmFinal;
                rota.KmPercorrido = request.KmFinal - rota.KmInicial;
                rota.DataFim = dataFim;
                rota.Ativo = false;

                var encerrada = await repository.UpdateAsync(rota);

                await AtualizarQuilometragemDoVeiculoAsync(encerrada.CodigoVeiculo, request.KmFinal);

                logger.LogInformation("Rota encerrada com sucesso. Id {Id} | Percorrido {Km} km",
                    encerrada.Id, encerrada.KmPercorrido);

                return encerrada.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao encerrar rota Id {Id}", command.Id);
                throw;
            }
        }

        /// <summary>
        /// Só avança o odômetro, nunca retrocede: uma rota encerrada com atraso não pode
        /// apagar uma quilometragem mais recente registrada por outro fluxo.
        /// </summary>
        private async Task AtualizarQuilometragemDoVeiculoAsync(int veiculoId, int kmFinal)
        {
            var veiculo = await veiculoRepository.GetByIdAsync(veiculoId, currentUser.EmpresaId);

            if (veiculo is null || kmFinal <= veiculo.Quilometragem)
                return;

            var anterior = veiculo.Quilometragem;
            veiculo.Quilometragem = kmFinal;
            await veiculoRepository.UpdateAsync(veiculo);

            logger.LogInformation("Quilometragem do veículo {VeiculoId} atualizada de {Anterior} para {Atual} pelo encerramento da rota",
                veiculoId, anterior, kmFinal);
        }
    }
}
