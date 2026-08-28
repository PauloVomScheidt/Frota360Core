using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Commands.DeleteRota
{
    public sealed class DeleteRotaHandler(IRotaRepository repository,
                                          ICurrentUserService currentUser,
                                          IAuditoriaService auditoria,
                                          ILogger<DeleteRotaHandler> logger)
        : ICommandHandler<DeleteRotaCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteRotaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção da rota Id {Id}", command.Id);

                var rota = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (rota is null)
                {
                    logger.LogWarning("Tentativa de remover rota inexistente. Id {Id}", command.Id);
                    return false;
                }

                await repository.DeleteAsync(rota);

                logger.LogInformation("Rota removida com sucesso. Id {Id}", command.Id);

                // Excluir rota é hoje o único jeito de desfazer um encerramento errado
                // (a API não expõe o caminho inverso) — vale registrar com o percurso inteiro.
                await auditoria.RegistrarAsync(EntidadesAuditadas.Rota, AcoesAuditoria.Excluiu, command.Id,
                    $"Excluiu a rota #{command.Id} ({rota.Origem} → {rota.Destino}) de {rota.Motorista?.Nome ?? "motorista removido"}");

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover rota Id {Id}", command.Id);
                throw;
            }
        }
    }
}
