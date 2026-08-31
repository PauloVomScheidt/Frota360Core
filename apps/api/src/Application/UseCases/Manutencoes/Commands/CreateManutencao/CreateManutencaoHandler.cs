using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Enums;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Manutencoes.Commands.CreateManutencao
{
    public sealed class CreateManutencaoHandler(IManutencaoRepository repository,
                                                IVeiculoRepository veiculoRepository,
                                                ITipoManutencaoRepository tipoRepository,
                                                ICurrentUserService currentUser,
                                                IAuditoriaService auditoria,
                                                ILogger<CreateManutencaoHandler> logger)
        : ICommandHandler<CreateManutencaoCommand, ManutencaoResponse>
    {
        public async Task<ManutencaoResponse> HandleAsync(CreateManutencaoCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;

            try
            {
                logger.LogInformation("Iniciando cadastro de manutenção do tipo {TipoId} para o veículo {VeiculoId}",
                    request.TipoManutencaoId, request.VeiculoId);

                // Buscas escopadas pela empresa do usuário: garantem que os ids vindos do corpo
                // não alcancem veículos ou tipos de outra empresa.
                var veiculo = await veiculoRepository.GetByIdAsync(request.VeiculoId, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException($"Veículo {request.VeiculoId} não encontrado.");

                var tipo = await tipoRepository.GetByIdAsync(request.TipoManutencaoId, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException($"Tipo de manutenção {request.TipoManutencaoId} não encontrado.");

                if (!tipo.Ativo)
                    throw new InvalidOperationException($"O tipo de manutenção \"{tipo.Nome}\" está inativo.");

                if (await repository.ExisteDuplicadaAsync(currentUser.EmpresaId, request.VeiculoId,
                        request.TipoManutencaoId, request.QuilometragemPrevista))
                    throw new InvalidOperationException(
                        $"Já existe manutenção pendente de \"{tipo.Nome}\" para este veículo em {request.QuilometragemPrevista} km.");

                var manutencao = new Manutencao
                {
                    EmpresaId = currentUser.EmpresaId,
                    VeiculoId = veiculo.Id,
                    TipoManutencaoId = tipo.Id,
                    QuilometragemPrevista = request.QuilometragemPrevista,
                    DataPrevista = request.DataPrevista,
                    Status = StatusManutencao.Pendente,
                    Observacao = request.Observacao,
                    DataInclusao = DateTime.Now
                };

                var criada = await repository.AddAsync(manutencao);

                // Navegações que o mapper usa (km restantes, nome do tipo) já estão em mãos.
                criada.Veiculo = veiculo;
                criada.Tipo = tipo;

                logger.LogInformation("Manutenção cadastrada com sucesso. Id {Id} | Veículo {VeiculoId} | Previsto {Km} km",
                    criada.Id, criada.VeiculoId, criada.QuilometragemPrevista);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Manutencao, AcoesAuditoria.Criou, criada.Id,
                    $"Agendou {tipo.Nome} para o veículo {veiculo.Placa} aos {criada.QuilometragemPrevista} km");

                return criada.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao cadastrar manutenção do tipo {TipoId} para o veículo {VeiculoId}",
                    request.TipoManutencaoId, request.VeiculoId);
                throw;
            }
        }
    }
}
