using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Rotas;
using Frota360.Domain.Common;
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
                                            IAuditoriaService auditoria,
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

                // Rota de outro motorista responde 404, não 403: para quem não é dono
                // dela, a rota simplesmente não existe.
                if (currentUser.EhMotorista() && rota.CodigoMotorista != currentUser.UsuarioId)
                {
                    logger.LogWarning("Motorista {MotoristaId} tentou encerrar a rota {Id}, que não é dele",
                        currentUser.UsuarioId, command.Id);
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

                var (placa, odometroAnterior) = await AtualizarVeiculoAsync(encerrada, request.KmFinal, dataFim);

                logger.LogInformation("Rota encerrada com sucesso. Id {Id} | Percorrido {Km} km",
                    encerrada.Id, encerrada.KmPercorrido);

                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Quilometragem final", null, encerrada.KmFinal)
                    .Comparar($"Odômetro do veículo {placa}", odometroAnterior, odometroAnterior is null ? null : request.KmFinal)
                    .Construir();

                await auditoria.RegistrarAsync(EntidadesAuditadas.Rota, AcoesAuditoria.Encerrou, encerrada.Id,
                    $"Encerrou a rota #{encerrada.Id} ({encerrada.Origem} → {encerrada.Destino}) com {encerrada.KmPercorrido} km rodados",
                    alteracoes);

                return encerrada.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao encerrar rota Id {Id}", command.Id);
                throw;
            }
        }

        /// <summary>
        /// Encerrar a rota é o momento em que o veículo tem informação nova: quem o levou,
        /// quando voltou e qual o odômetro. Os três só avançam, nunca retrocedem — uma
        /// rota encerrada com atraso não pode apagar um registro mais recente.
        /// </summary>
        /// <returns>
        /// A placa (para a descrição da auditoria) e o odômetro anterior quando ele avançou —
        /// nulo quando o número do encerramento não superou o que já estava lá.
        /// </returns>
        private async Task<(string Placa, int? OdometroAnterior)> AtualizarVeiculoAsync(
            Domain.Entities.Rota rota, int kmFinal, DateTime dataFim)
        {
            var veiculo = await veiculoRepository.GetByIdAsync(rota.CodigoVeiculo, currentUser.EmpresaId);

            if (veiculo is null)
                return ($"#{rota.CodigoVeiculo}", null);

            var mudou = false;
            int? odometroAnterior = null;

            if (kmFinal > veiculo.Quilometragem)
            {
                logger.LogInformation("Quilometragem do veículo {VeiculoId} atualizada de {Anterior} para {Atual} pelo encerramento da rota",
                    veiculo.Id, veiculo.Quilometragem, kmFinal);
                odometroAnterior = veiculo.Quilometragem;
                veiculo.Quilometragem = kmFinal;
                mudou = true;
            }

            // Última viagem: só a rota mais recente manda. Sem esta guarda, encerrar hoje
            // uma rota de mês passado reescreveria o veículo com dado velho.
            if (veiculo.DataUltimaViagem is null || dataFim > veiculo.DataUltimaViagem)
            {
                veiculo.DataUltimaViagem = dataFim;
                veiculo.UltimoMotorista = rota.Motorista?.Nome;
                mudou = true;
            }

            if (mudou)
                await veiculoRepository.UpdateAsync(veiculo);

            return (veiculo.Placa, odometroAnterior);
        }
    }
}
