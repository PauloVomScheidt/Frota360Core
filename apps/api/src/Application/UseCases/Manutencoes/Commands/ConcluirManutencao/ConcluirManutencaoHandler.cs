using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Enums;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Manutencoes.Commands.ConcluirManutencao
{
    /// <summary>
    /// Fecha o ciclo da manutenção: registra o que foi executado de fato e, de quebra,
    /// aproveita a quilometragem informada para manter o veículo atualizado — é o momento
    /// em que alguém olhou o odômetro de verdade.
    /// </summary>
    public sealed class ConcluirManutencaoHandler(IManutencaoRepository repository,
                                                  IVeiculoRepository veiculoRepository,
                                                  ICurrentUserService currentUser,
                                                  IAuditoriaService auditoria,
                                                  ILogger<ConcluirManutencaoHandler> logger)
        : ICommandHandler<ConcluirManutencaoCommand, ManutencaoResponse?>
    {
        public async Task<ManutencaoResponse?> HandleAsync(ConcluirManutencaoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando conclusão da manutenção Id {Id}", command.Id);

                var manutencao = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (manutencao is null)
                {
                    logger.LogWarning("Tentativa de concluir manutenção inexistente. Id {Id}", command.Id);
                    return null;
                }

                if (manutencao.Status != StatusManutencao.Pendente)
                    throw new InvalidOperationException(
                        $"Manutenção {command.Id} já está {manutencao.Status.ToString().ToLowerInvariant()}.");

                var request = command.Data;

                manutencao.Status = StatusManutencao.Realizada;
                manutencao.QuilometragemRealizada = request.QuilometragemRealizada;
                manutencao.DataRealizacao = request.DataRealizacao;
                manutencao.Custo = request.Custo;

                if (!string.IsNullOrWhiteSpace(request.Observacao))
                    manutencao.Observacao = request.Observacao;

                var concluida = await repository.UpdateAsync(manutencao);

                var (placa, odometroAnterior) =
                    await AtualizarQuilometragemDoVeiculoAsync(concluida.VeiculoId, request.QuilometragemRealizada);

                logger.LogInformation("Manutenção concluída com sucesso. Id {Id} | Realizada em {Km} km",
                    concluida.Id, concluida.QuilometragemRealizada);

                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Status", StatusManutencao.Pendente.ToString(), StatusManutencao.Realizada.ToString())
                    .Comparar("Quilometragem realizada", null, concluida.QuilometragemRealizada)
                    .Comparar("Custo", null, concluida.Custo)
                    .Comparar($"Odômetro do veículo {placa}", odometroAnterior, odometroAnterior is null ? null : request.QuilometragemRealizada)
                    .Construir();

                await auditoria.RegistrarAsync(EntidadesAuditadas.Manutencao, AcoesAuditoria.Concluiu, concluida.Id,
                    $"Concluiu a manutenção #{concluida.Id} ({concluida.Tipo?.Nome}, veículo {placa}) aos {concluida.QuilometragemRealizada} km",
                    alteracoes);

                return concluida.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao concluir manutenção Id {Id}", command.Id);
                throw;
            }
        }

        /// <summary>
        /// Só avança o odômetro, nunca retrocede: uma conclusão lançada com atraso não pode
        /// apagar uma quilometragem mais recente registrada por outro fluxo.
        /// </summary>
        /// <returns>
        /// A placa (para a descrição da auditoria) e o odômetro anterior quando ele avançou —
        /// nulo quando a conclusão não superou o que já estava lá.
        /// </returns>
        private async Task<(string Placa, int? OdometroAnterior)> AtualizarQuilometragemDoVeiculoAsync(
            int veiculoId, int quilometragemRealizada)
        {
            var veiculo = await veiculoRepository.GetByIdAsync(veiculoId, currentUser.EmpresaId);

            if (veiculo is null)
                return ($"#{veiculoId}", null);

            if (quilometragemRealizada <= veiculo.Quilometragem)
                return (veiculo.Placa, null);

            var anterior = veiculo.Quilometragem;
            veiculo.Quilometragem = quilometragemRealizada;
            await veiculoRepository.UpdateAsync(veiculo);

            logger.LogInformation("Quilometragem do veículo {VeiculoId} atualizada de {Anterior} para {Atual} pela conclusão da manutenção",
                veiculoId, anterior, quilometragemRealizada);

            return (veiculo.Placa, anterior);
        }
    }
}
