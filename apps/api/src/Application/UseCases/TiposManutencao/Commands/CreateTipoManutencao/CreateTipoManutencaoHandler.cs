using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoManutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.TiposManutencao.Commands.CreateTipoManutencao
{
    public sealed class CreateTipoManutencaoHandler(ITipoManutencaoRepository repository,
                                                    ICurrentUserService currentUser,
                                                    IAuditoriaService auditoria,
                                                    ILogger<CreateTipoManutencaoHandler> logger)
        : ICommandHandler<CreateTipoManutencaoCommand, TipoManutencaoResponse>
    {
        public async Task<TipoManutencaoResponse> HandleAsync(CreateTipoManutencaoCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;
            var nome = request.Nome.Trim();

            try
            {
                logger.LogInformation("Iniciando cadastro do tipo de manutenção {Nome}", nome);

                // Checagem explícita para devolver mensagem legível em vez do erro do índice único.
                if (await repository.ExisteNomeAsync(currentUser.EmpresaId, nome))
                    throw new InvalidOperationException($"Já existe um tipo de manutenção chamado \"{nome}\".");

                var criado = await repository.AddAsync(new TipoManutencao
                {
                    EmpresaId = currentUser.EmpresaId,
                    Nome = nome,
                    IntervaloKm = request.IntervaloKm,
                    Ativo = true,
                    DataInclusao = DateTime.UtcNow
                });

                logger.LogInformation("Tipo de manutenção cadastrado com sucesso. Id {Id} | Nome {Nome}", criado.Id, criado.Nome);

                var intervalo = criado.IntervaloKm is null ? "sem intervalo definido" : $"a cada {criado.IntervaloKm} km";
                await auditoria.RegistrarAsync(EntidadesAuditadas.TipoManutencao, AcoesAuditoria.Criou, criado.Id,
                    $"Criou o tipo de manutenção \"{criado.Nome}\" ({intervalo})");

                return criado.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao cadastrar tipo de manutenção {Nome}", nome);
                throw;
            }
        }
    }
}
