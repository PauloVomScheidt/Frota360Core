using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoCombustivel.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposCombustivel.Commands.CreateTipoCombustivel
{
    public sealed class CreateTipoCombustivelHandler(ITipoCombustivelRepository repository,
                                                 ICurrentUserService currentUser,
                                                 IAuditoriaService auditoria,
                                                 ILogger<CreateTipoCombustivelHandler> logger)
        : ICommandHandler<CreateTipoCombustivelCommand, TipoCombustivelResponse>
    {
        public async Task<TipoCombustivelResponse> HandleAsync(CreateTipoCombustivelCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;
            var nome = request.Nome.Trim();

            try
            {
                logger.LogInformation("Iniciando cadastro do tipo de combustível {Nome}", nome);

                // Checagem explícita para devolver mensagem legível em vez do erro do índice único.
                if (await repository.ExisteNomeAsync(currentUser.EmpresaId, nome))
                    throw new InvalidOperationException($"Já existe um tipo de combustível chamado \"{nome}\".");

                var criado = await repository.AddAsync(new TipoCombustivel
                {
                    EmpresaId = currentUser.EmpresaId,
                    Nome = nome,
                    Ativo = true,
                    DataInclusao = DateTime.Now
                });

                logger.LogInformation("Tipo de combustível cadastrado com sucesso. Id {Id} | Nome {Nome}", criado.Id, criado.Nome);

                await auditoria.RegistrarAsync(EntidadesAuditadas.TipoCombustivel, AcoesAuditoria.Criou, criado.Id,
                    $"Criou o tipo de combustível \"{criado.Nome}\"");

                return criado.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao cadastrar tipo de combustível {Nome}", nome);
                throw;
            }
        }
    }
}
