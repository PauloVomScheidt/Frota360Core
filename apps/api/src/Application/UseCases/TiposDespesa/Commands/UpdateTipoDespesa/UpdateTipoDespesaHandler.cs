using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.TipoDespesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposDespesa.Commands.UpdateTipoDespesa
{
    public sealed class UpdateTipoDespesaHandler(ITipoDespesaRepository repository,
                                                 ICurrentUserService currentUser,
                                                 IAuditoriaService auditoria,
                                                 ILogger<UpdateTipoDespesaHandler> logger)
        : ICommandHandler<UpdateTipoDespesaCommand, TipoDespesaResponse?>
    {
        public async Task<TipoDespesaResponse?> HandleAsync(UpdateTipoDespesaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização do tipo de despesa Id {Id}", command.Id);

                var tipo = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (tipo is null)
                {
                    logger.LogWarning("Tentativa de atualizar tipo de despesa inexistente. Id {Id}", command.Id);
                    return null;
                }

                var request = command.Data;
                var nome = request.Nome.Trim();

                if (await repository.ExisteNomeAsync(currentUser.EmpresaId, nome, ignorarId: tipo.Id))
                    throw new InvalidOperationException($"Já existe um tipo de despesa chamado \"{nome}\".");

                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Nome", tipo.Nome, nome)
                    .Comparar("Ativo", tipo.Ativo, request.Ativo)
                    .Construir();

                tipo.Nome = nome;
                tipo.Ativo = request.Ativo;

                var atualizado = await repository.UpdateAsync(tipo);

                logger.LogInformation("Tipo de despesa atualizado com sucesso. Id {Id}", atualizado.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.TipoDespesa, AcoesAuditoria.Atualizou, atualizado.Id,
                    $"Atualizou o tipo de despesa \"{atualizado.Nome}\"", alteracoes);

                return atualizado.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao atualizar tipo de despesa Id {Id}", command.Id);
                throw;
            }
        }
    }
}
