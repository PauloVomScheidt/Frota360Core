using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoDespesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposDespesa.Commands.CreateTipoDespesa
{
    public sealed class CreateTipoDespesaHandler(ITipoDespesaRepository repository,
                                                 ICurrentUserService currentUser,
                                                 IAuditoriaService auditoria,
                                                 ILogger<CreateTipoDespesaHandler> logger)
        : ICommandHandler<CreateTipoDespesaCommand, TipoDespesaResponse>
    {
        public async Task<TipoDespesaResponse> HandleAsync(CreateTipoDespesaCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;
            var nome = request.Nome.Trim();

            try
            {
                logger.LogInformation("Iniciando cadastro do tipo de despesa {Nome}", nome);

                // Checagem explícita para devolver mensagem legível em vez do erro do índice único.
                if (await repository.ExisteNomeAsync(currentUser.EmpresaId, nome))
                    throw new InvalidOperationException($"Já existe um tipo de despesa chamado \"{nome}\".");

                var criado = await repository.AddAsync(new TipoDespesa
                {
                    EmpresaId = currentUser.EmpresaId,
                    Nome = nome,
                    Ativo = true,
                    DataInclusao = DateTime.Now
                });

                logger.LogInformation("Tipo de despesa cadastrado com sucesso. Id {Id} | Nome {Nome}", criado.Id, criado.Nome);

                await auditoria.RegistrarAsync(EntidadesAuditadas.TipoDespesa, AcoesAuditoria.Criou, criado.Id,
                    $"Criou o tipo de despesa \"{criado.Nome}\"");

                return criado.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao cadastrar tipo de despesa {Nome}", nome);
                throw;
            }
        }
    }
}
