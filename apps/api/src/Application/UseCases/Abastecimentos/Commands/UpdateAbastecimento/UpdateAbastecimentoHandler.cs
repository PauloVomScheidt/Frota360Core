using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Abastecimentos.Commands.UpdateAbastecimento
{
    public sealed class UpdateAbastecimentoHandler(IAbastecimentoRepository repository,
                                                   ICurrentUserService currentUser,
                                                   IAuditoriaService auditoria,
                                                   ILogger<UpdateAbastecimentoHandler> logger)
        : ICommandHandler<UpdateAbastecimentoCommand, AbastecimentoResponse?>
    {
        public async Task<AbastecimentoResponse?> HandleAsync(UpdateAbastecimentoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando correção do abastecimento Id {Id}", command.Id);

                var abastecimento = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (abastecimento is null)
                {
                    logger.LogWarning("Tentativa de corrigir abastecimento inexistente. Id {Id}", command.Id);
                    return null;
                }

                // Lançamento de outro motorista responde 404, não 403: para quem não é o dono
                // do gasto, ele simplesmente não existe — mesma regra da rota alheia.
                if (currentUser.EhMotorista() && abastecimento.MotoristaId != currentUser.UsuarioId)
                {
                    logger.LogWarning("Motorista {UsuarioId} tentou corrigir o abastecimento {Id}, que não é dele",
                        currentUser.UsuarioId, command.Id);
                    return null;
                }

                var request = command.Data;

                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Valor", abastecimento.Valor, request.Valor)
                    .Comparar("Data", abastecimento.DataAbastecimento, request.DataAbastecimento)
                    .Comparar("Observação", abastecimento.Observacao, request.Observacao)
                    .Construir();

                // Veículo, motorista e rota ficam de fora do PUT: trocar qualquer um reescreveria
                // a atribuição do gasto (ver UpdateAbastecimentoRequest).
                abastecimento.Valor = request.Valor;
                abastecimento.DataAbastecimento = request.DataAbastecimento;
                abastecimento.Observacao = request.Observacao;

                await repository.UpdateAsync(abastecimento);

                logger.LogInformation("Abastecimento corrigido com sucesso. Id {Id}", command.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Abastecimento, AcoesAuditoria.Atualizou, command.Id,
                    $"Corrigiu o abastecimento #{command.Id} do veículo {abastecimento.Veiculo?.Placa}", alteracoes);

                var atualizado = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                return atualizado!.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao corrigir abastecimento Id {Id}", command.Id);
                throw;
            }
        }
    }
}
