using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Despesas.Commands.CreateDespesa
{
    public sealed class CreateDespesaHandler(IDespesaRepository repository,
                                             IVeiculoRepository veiculoRepository,
                                             ITipoDespesaRepository tipoDespesaRepository,
                                             IUsuarioRepository usuarioRepository,
                                             ICurrentUserService currentUser,
                                             IAuditoriaService auditoria,
                                             ILogger<CreateDespesaHandler> logger)
        : ICommandHandler<CreateDespesaCommand, DespesaResponse>
    {
        public async Task<DespesaResponse> HandleAsync(CreateDespesaCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;

            try
            {
                logger.LogInformation("Iniciando lançamento de despesa | Veículo {VeiculoId} | Tipo {TipoId} | Valor {Valor}",
                    request.VeiculoId, request.TipoDespesaId, request.Valor);

                var veiculo = await ResolverVeiculoAsync(request.VeiculoId);
                var tipo = await ResolverTipoAsync(request.TipoDespesaId, exigirAtivo: true);
                var motorista = await ResolverMotoristaAsync(request.MotoristaId);

                var criada = await repository.AddAsync(new Despesa
                {
                    EmpresaId = currentUser.EmpresaId,
                    VeiculoId = veiculo.Id,
                    TipoDespesaId = tipo.Id,
                    MotoristaId = motorista?.Id,
                    Valor = request.Valor,
                    DataDespesa = request.DataDespesa,
                    Observacao = request.Observacao,
                    DataInclusao = DateTime.Now
                });

                // As navegações não vêm preenchidas pelo AddAsync; a resposta é desnormalizada,
                // então atribuímos à mão — mesmo padrão dos handlers de rota.
                criada.Veiculo = veiculo;
                criada.Tipo = tipo;
                criada.Motorista = motorista;

                logger.LogInformation("Despesa lançada com sucesso. Id {Id}", criada.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Despesa, AcoesAuditoria.Criou, criada.Id,
                    $"Lançou {tipo.Nome} de R$ {criada.Valor:0.00} no veículo {veiculo.Placa}");

                return criada.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao lançar despesa no veículo {VeiculoId}", request.VeiculoId);
                throw;
            }
        }

        private async Task<Veiculo> ResolverVeiculoAsync(int veiculoId)
            => await veiculoRepository.GetByIdAsync(veiculoId, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Veículo {veiculoId} não encontrado.");

        /// <summary>
        /// Tipo aposentado não recebe lançamento novo — mas continua nomeando os antigos, e
        /// por isso a edição só cobra <paramref name="exigirAtivo"/> quando o tipo muda.
        /// </summary>
        private async Task<TipoDespesa> ResolverTipoAsync(int tipoId, bool exigirAtivo)
        {
            var tipo = await tipoDespesaRepository.GetByIdAsync(tipoId, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Tipo de despesa {tipoId} não encontrado.");

            if (exigirAtivo && !tipo.Ativo)
                throw new InvalidOperationException($"O tipo \"{tipo.Nome}\" está inativo e não aceita lançamentos novos.");

            return tipo;
        }

        /// <summary>
        /// Opcional: multa tem dono, IPVA não. <c>GetMotoristaByIdAsync</c> filtra empresa
        /// <b>e</b> role, então um Supervisor informado aqui simplesmente "não existe".
        /// </summary>
        private async Task<Usuario?> ResolverMotoristaAsync(int? motoristaId)
        {
            if (motoristaId is not int id)
                return null;

            return await usuarioRepository.GetMotoristaByIdAsync(id, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Motorista {id} não encontrado.");
        }
    }
}
