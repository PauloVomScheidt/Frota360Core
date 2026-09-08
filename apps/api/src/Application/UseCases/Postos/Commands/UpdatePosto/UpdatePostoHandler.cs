using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Posto.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Postos.Commands.UpdatePosto
{
    public sealed class UpdatePostoHandler(IPostoRepository repository,
                                           ICurrentUserService currentUser,
                                           IAuditoriaService auditoria,
                                           ILogger<UpdatePostoHandler> logger)
        : ICommandHandler<UpdatePostoCommand, PostoResponse?>
    {
        public async Task<PostoResponse?> HandleAsync(UpdatePostoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização do posto Id {Id}", command.Id);

                var posto = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (posto is null)
                {
                    logger.LogWarning("Tentativa de atualizar posto inexistente. Id {Id}", command.Id);
                    return null;
                }

                var request = command.Data;
                var nome = request.Nome.Trim();
                var cnpj = string.IsNullOrWhiteSpace(request.Cnpj) ? null : request.Cnpj.Trim();
                var cidade = string.IsNullOrWhiteSpace(request.Cidade) ? null : request.Cidade.Trim();

                if (await repository.ExisteNomeAsync(currentUser.EmpresaId, nome, ignorarId: posto.Id))
                    throw new InvalidOperationException($"Já existe um posto chamado \"{nome}\".");

                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Nome", posto.Nome, nome)
                    .Comparar("CNPJ", posto.Cnpj, cnpj)
                    .Comparar("Cidade", posto.Cidade, cidade)
                    .Comparar("Ativo", posto.Ativo, request.Ativo)
                    .Construir();

                posto.Nome = nome;
                posto.Cnpj = cnpj;
                posto.Cidade = cidade;
                posto.Ativo = request.Ativo;

                var atualizado = await repository.UpdateAsync(posto);

                logger.LogInformation("Posto atualizado com sucesso. Id {Id}", atualizado.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Posto, AcoesAuditoria.Atualizou, atualizado.Id,
                    $"Atualizou o posto \"{atualizado.Nome}\"", alteracoes);

                return atualizado.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao atualizar posto Id {Id}", command.Id);
                throw;
            }
        }
    }
}
