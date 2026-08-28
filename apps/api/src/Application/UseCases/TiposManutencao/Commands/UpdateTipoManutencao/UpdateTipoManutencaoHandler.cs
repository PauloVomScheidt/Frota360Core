using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.TipoManutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposManutencao.Commands.UpdateTipoManutencao
{
    public sealed class UpdateTipoManutencaoHandler(ITipoManutencaoRepository repository,
                                                    ICurrentUserService currentUser,
                                                    IAuditoriaService auditoria,
                                                    ILogger<UpdateTipoManutencaoHandler> logger)
        : ICommandHandler<UpdateTipoManutencaoCommand, TipoManutencaoResponse?>
    {
        public async Task<TipoManutencaoResponse?> HandleAsync(UpdateTipoManutencaoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização do tipo de manutenção Id {Id}", command.Id);

                var tipo = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (tipo is null)
                {
                    logger.LogWarning("Tentativa de atualizar tipo de manutenção inexistente. Id {Id}", command.Id);
                    return null;
                }

                var request = command.Data;
                var nome = request.Nome.Trim();

                if (await repository.ExisteNomeAsync(currentUser.EmpresaId, nome, ignorarId: tipo.Id))
                    throw new InvalidOperationException($"Já existe um tipo de manutenção chamado \"{nome}\".");

                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Nome", tipo.Nome, nome)
                    .Comparar("Intervalo (km)", tipo.IntervaloKm, request.IntervaloKm)
                    .Comparar("Ativo", tipo.Ativo, request.Ativo)
                    .Construir();

                tipo.Nome = nome;
                tipo.IntervaloKm = request.IntervaloKm;
                tipo.Ativo = request.Ativo;

                var atualizado = await repository.UpdateAsync(tipo);

                logger.LogInformation("Tipo de manutenção atualizado com sucesso. Id {Id}", atualizado.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.TipoManutencao, AcoesAuditoria.Atualizou, atualizado.Id,
                    $"Editou o tipo de manutenção \"{atualizado.Nome}\"", alteracoes);

                return atualizado.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao atualizar tipo de manutenção Id {Id}", command.Id);
                throw;
            }
        }
    }
}
