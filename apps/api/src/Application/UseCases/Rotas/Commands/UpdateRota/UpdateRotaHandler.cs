using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Rotas;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Commands.UpdateRota
{
    public sealed class UpdateRotaHandler(IRotaRepository repository,
                                          IUsuarioRepository usuarioRepository,
                                          IVeiculoRepository veiculoRepository,
                                          ICurrentUserService currentUser,
                                          IAuditoriaService auditoria,
                                          ILogger<UpdateRotaHandler> logger)
        : ICommandHandler<UpdateRotaCommand, RotaResponse?>
    {
        public async Task<RotaResponse?> HandleAsync(UpdateRotaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização da rota Id {Id}", command.Id);

                var rota = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (rota is null)
                {
                    logger.LogWarning("Tentativa de atualizar rota inexistente. Id {Id}", command.Id);
                    return null;
                }

                var request = command.Data;

                // Buscas escopadas pela empresa do usuário: garantem que os ids vindos do corpo
                // não alcancem usuários ou veículos de outra empresa. Quem não tem a role
                // Motorista também "não existe" aqui — rota só é atribuível a motorista.
                var motorista = await usuarioRepository.GetMotoristaByIdAsync(request.CodigoMotorista, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException($"Motorista {request.CodigoMotorista} não encontrado.");

                var veiculo = await veiculoRepository.GetByIdAsync(request.CodigoVeiculo, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException($"Veículo {request.CodigoVeiculo} não encontrado.");

                // O diff sai antes da mutação. O motorista entra pelo nome, não pelo id:
                // "de João para Maria" é o que o Admin precisa ler na tela.
                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Partida", rota.EnderecoPartida, request.EnderecoPartida)
                    .Comparar("Chegada", rota.EnderecoChegada, request.EnderecoChegada)
                    .Comparar("Motorista", rota.Motorista?.Nome ?? $"#{rota.CodigoMotorista}", motorista.Nome)
                    .Comparar("Veículo", rota.Veiculo?.Placa ?? $"#{rota.CodigoVeiculo}", veiculo.Placa)
                    .Comparar("Data de início", rota.DataInicio, request.DataInicio)
                    .Construir();

                // Ativo/DataFim/KmFinal ficam de fora: quem move o estado da rota é o encerrar.
                rota.EnderecoPartida = request.EnderecoPartida;
                rota.EnderecoChegada = request.EnderecoChegada;

                // Coordenada só é sobrescrita quando vem no corpo: uma edição de texto não
                // pode apagar o traçado já calculado.
                if (request.LatitudePartida is not null && request.LongitudePartida is not null)
                {
                    rota.LatitudePartida = request.LatitudePartida.Value;
                    rota.LongitudePartida = request.LongitudePartida.Value;
                }

                if (request.LatitudeChegada is not null && request.LongitudeChegada is not null)
                {
                    rota.LatitudeChegada = request.LatitudeChegada.Value;
                    rota.LongitudeChegada = request.LongitudeChegada.Value;
                }

                // Traçado novo só sobrescreve o antigo quando vem de fato: uma edição que
                // mexe só no motorista não pode apagar o trajeto já calculado.
                if (request.DistanciaMetros is not null)
                {
                    rota.DistanciaMetros = request.DistanciaMetros.Value;
                    rota.DuracaoEstimadaSegundos = request.DuracaoEstimadaSegundos ?? 0;
                    rota.PolylineCodificada = request.PolylineCodificada;
                    rota.DataCalculoRota = DateTime.Now;
                }
                rota.CodigoMotorista = motorista.Id;
                rota.CodigoVeiculo = veiculo.Id;
                rota.DataInicio = request.DataInicio;

                var atualizado = await repository.UpdateAsync(rota);

                logger.LogInformation("Rota atualizada com sucesso. Id {Id}", atualizado.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Rota, AcoesAuditoria.Atualizou, atualizado.Id,
                    $"Atualizou a rota #{atualizado.Id} ({atualizado.EnderecoPartida} → {atualizado.EnderecoChegada})", alteracoes);

                var resposta = atualizado.ToResponse();
                // A navegação carregada ainda aponta para o motorista anterior quando a
                // rota troca de dono; o nome certo é o do que acabou de ser resolvido.
                resposta.NomeMotorista = motorista.Nome;
                return resposta;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao atualizar rota Id {Id}", command.Id);
                throw;
            }
        }
    }
}
