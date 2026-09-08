using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.TipoCombustivel.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposCombustivel.Commands.UpdateTipoCombustivel
{
    public sealed class UpdateTipoCombustivelHandler(ITipoCombustivelRepository repository,
                                                 ICurrentUserService currentUser,
                                                 IAuditoriaService auditoria,
                                                 ILogger<UpdateTipoCombustivelHandler> logger)
        : ICommandHandler<UpdateTipoCombustivelCommand, TipoCombustivelResponse?>
    {
        public async Task<TipoCombustivelResponse?> HandleAsync(UpdateTipoCombustivelCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização do tipo de combustível Id {Id}", command.Id);

                var tipo = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (tipo is null)
                {
                    logger.LogWarning("Tentativa de atualizar tipo de combustível inexistente. Id {Id}", command.Id);
                    return null;
                }

                var request = command.Data;
                var nome = request.Nome.Trim();

                if (await repository.ExisteNomeAsync(currentUser.EmpresaId, nome, ignorarId: tipo.Id))
                    throw new InvalidOperationException($"Já existe um tipo de combustível chamado \"{nome}\".");

                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Nome", tipo.Nome, nome)
                    .Comparar("Ativo", tipo.Ativo, request.Ativo)
                    .Construir();

                tipo.Nome = nome;
                tipo.Ativo = request.Ativo;

                var atualizado = await repository.UpdateAsync(tipo);

                logger.LogInformation("Tipo de combustível atualizado com sucesso. Id {Id}", atualizado.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.TipoCombustivel, AcoesAuditoria.Atualizou, atualizado.Id,
                    $"Atualizou o tipo de combustível \"{atualizado.Nome}\"", alteracoes);

                return atualizado.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao atualizar tipo de combustível Id {Id}", command.Id);
                throw;
            }
        }
    }
}
