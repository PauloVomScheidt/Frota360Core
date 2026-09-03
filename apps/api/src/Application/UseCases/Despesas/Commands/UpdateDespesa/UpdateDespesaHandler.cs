using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Despesas.Commands.UpdateDespesa
{
    public sealed class UpdateDespesaHandler(IDespesaRepository repository,
                                             IVeiculoRepository veiculoRepository,
                                             ITipoDespesaRepository tipoDespesaRepository,
                                             IUsuarioRepository usuarioRepository,
                                             ICurrentUserService currentUser,
                                             IAuditoriaService auditoria,
                                             ILogger<UpdateDespesaHandler> logger)
        : ICommandHandler<UpdateDespesaCommand, DespesaResponse?>
    {
        public async Task<DespesaResponse?> HandleAsync(UpdateDespesaCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando atualização da despesa Id {Id}", command.Id);

                var despesa = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (despesa is null)
                {
                    logger.LogWarning("Tentativa de atualizar despesa inexistente. Id {Id}", command.Id);
                    return null;
                }

                var request = command.Data;

                var veiculo = await ResolverVeiculoAsync(request.VeiculoId);
                // Só cobra "ativo" quando o tipo muda: senão corrigir o valor de uma despesa
                // antiga quebraria por causa de um tipo aposentado depois do lançamento.
                var tipo = await ResolverTipoAsync(request.TipoDespesaId,
                    exigirAtivo: request.TipoDespesaId != despesa.TipoDespesaId);
                var motorista = await ResolverMotoristaAsync(request.MotoristaId);

                // Diff montado antes da mutação — depois o "antes" se perde.
                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Veículo", despesa.Veiculo?.Placa, veiculo.Placa)
                    .Comparar("Tipo", despesa.Tipo?.Nome, tipo.Nome)
                    .Comparar("Motorista", despesa.Motorista?.Nome, motorista?.Nome)
                    .Comparar("Valor", despesa.Valor, request.Valor)
                    .Comparar("Data", despesa.DataDespesa, request.DataDespesa)
                    .Comparar("Observação", despesa.Observacao, request.Observacao)
                    .Construir();

                despesa.VeiculoId = veiculo.Id;
                despesa.TipoDespesaId = tipo.Id;
                despesa.MotoristaId = motorista?.Id;
                despesa.Valor = request.Valor;
                despesa.DataDespesa = request.DataDespesa;
                despesa.Observacao = request.Observacao;

                var atualizada = await repository.UpdateAsync(despesa);

                atualizada.Veiculo = veiculo;
                atualizada.Tipo = tipo;
                atualizada.Motorista = motorista;

                logger.LogInformation("Despesa atualizada com sucesso. Id {Id}", atualizada.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Despesa, AcoesAuditoria.Atualizou, atualizada.Id,
                    $"Atualizou a despesa #{atualizada.Id} ({tipo.Nome}) do veículo {veiculo.Placa}", alteracoes);

                return atualizada.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao atualizar despesa Id {Id}", command.Id);
                throw;
            }
        }

        private async Task<Veiculo> ResolverVeiculoAsync(int veiculoId)
            => await veiculoRepository.GetByIdAsync(veiculoId, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Veículo {veiculoId} não encontrado.");

        private async Task<TipoDespesa> ResolverTipoAsync(int tipoId, bool exigirAtivo)
        {
            var tipo = await tipoDespesaRepository.GetByIdAsync(tipoId, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Tipo de despesa {tipoId} não encontrado.");

            if (exigirAtivo && !tipo.Ativo)
                throw new InvalidOperationException($"O tipo \"{tipo.Nome}\" está inativo e não aceita lançamentos novos.");

            return tipo;
        }

        private async Task<Usuario?> ResolverMotoristaAsync(int? motoristaId)
        {
            if (motoristaId is not int id)
                return null;

            return await usuarioRepository.GetMotoristaByIdAsync(id, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Motorista {id} não encontrado.");
        }
    }
}
