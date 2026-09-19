using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Commands.CalcularRota
{
    public sealed class CalcularRotaHandler(IGoogleRoutesClient routesClient,
                                            ICurrentUserService currentUser,
                                            IAuditoriaService auditoria,
                                            ILogger<CalcularRotaHandler> logger)
        : ICommandHandler<CalcularRotaCommand, CalculoRotaResponse>
    {
        public async Task<CalculoRotaResponse> HandleAsync(CalcularRotaCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;

            try
            {
                logger.LogInformation("Iniciando cálculo de rota para a empresa {EmpresaId}", currentUser.EmpresaId);

                var origem = new Coordenada(request.LatitudeOrigem, request.LongitudeOrigem);
                var destino = new Coordenada(request.LatitudeDestino, request.LongitudeDestino);

                var resultado = await routesClient.CalcularAsync(origem, destino, cancellationToken);

                // O cliente não lança: a falha chega como resultado e vira 422 com texto de
                // usuário. O motivo técnico fica no log — quem está na tela não precisa saber
                // se foi timeout ou 403 da Google.
                if (!resultado.Sucesso)
                {
                    logger.LogWarning("Cálculo de rota não concluído para a empresa {EmpresaId}. Motivo: {Motivo}",
                        currentUser.EmpresaId, resultado.Motivo);

                    throw new InvalidOperationException(
                        "Não foi possível calcular o trajeto agora. Tente novamente em instantes.");
                }

                logger.LogInformation("Rota calculada para a empresa {EmpresaId}: {Metros} m em {Segundos} s",
                    currentUser.EmpresaId, resultado.DistanciaMetros, resultado.DuracaoEstimadaSegundos);

                // Trilha da chamada cobrada. EmpresaId, usuário e data/hora saem do
                // AuditoriaService; aqui só entra o que permite conferir a conta depois.
                // EntidadeId é nulo porque a rota ainda não existe — o cálculo precede o
                // cadastro. Só o sucesso é registrado: falha de rede não é chamada faturada.
                await auditoria.RegistrarAsync(EntidadesAuditadas.Rota, AcoesAuditoria.Calculou, null,
                    $"Calculou trajeto de ({request.LatitudeOrigem}, {request.LongitudeOrigem}) " +
                    $"para ({request.LatitudeDestino}, {request.LongitudeDestino}) — " +
                    $"{resultado.DistanciaMetros} m");

                return new CalculoRotaResponse
                {
                    DistanciaMetros = resultado.DistanciaMetros,
                    DuracaoEstimadaSegundos = resultado.DuracaoEstimadaSegundos,
                    PolylineCodificada = resultado.PolylineCodificada
                };
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao calcular rota para a empresa {EmpresaId}", currentUser.EmpresaId);
                throw;
            }
        }
    }
}
