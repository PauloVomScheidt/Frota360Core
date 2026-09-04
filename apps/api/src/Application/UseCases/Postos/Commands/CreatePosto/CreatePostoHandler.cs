using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Posto.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Postos.Commands.CreatePosto
{
    public sealed class CreatePostoHandler(IPostoRepository repository,
                                           ICurrentUserService currentUser,
                                           IAuditoriaService auditoria,
                                           ILogger<CreatePostoHandler> logger)
        : ICommandHandler<CreatePostoCommand, PostoResponse>
    {
        public async Task<PostoResponse> HandleAsync(CreatePostoCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;
            var nome = request.Nome.Trim();

            try
            {
                logger.LogInformation("Iniciando cadastro do posto {Nome}", nome);

                // Checagem explícita para devolver mensagem legível em vez do erro do índice único.
                if (await repository.ExisteNomeAsync(currentUser.EmpresaId, nome))
                    throw new InvalidOperationException($"Já existe um posto chamado \"{nome}\".");

                var criado = await repository.AddAsync(new Posto
                {
                    EmpresaId = currentUser.EmpresaId,
                    Nome = nome,
                    Cnpj = string.IsNullOrWhiteSpace(request.Cnpj) ? null : request.Cnpj.Trim(),
                    Cidade = string.IsNullOrWhiteSpace(request.Cidade) ? null : request.Cidade.Trim(),
                    Ativo = true,
                    DataInclusao = DateTime.Now
                });

                logger.LogInformation("Posto cadastrado com sucesso. Id {Id} | Nome {Nome}", criado.Id, criado.Nome);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Posto, AcoesAuditoria.Criou, criado.Id,
                    $"Credenciou o posto \"{criado.Nome}\"");

                return criado.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao cadastrar posto {Nome}", nome);
                throw;
            }
        }
    }
}
