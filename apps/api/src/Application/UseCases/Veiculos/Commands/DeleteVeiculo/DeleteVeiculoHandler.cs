using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Veiculos.Commands.DeleteVeiculo
{
    public sealed class DeleteVeiculoHandler(IVeiculoRepository repository,
                                             IRotaRepository rotaRepository,
                                             IAbastecimentoRepository abastecimentoRepository,
                                             ICurrentUserService currentUser,
                                             IAuditoriaService auditoria,
                                             ILogger<DeleteVeiculoHandler> logger)
        : ICommandHandler<DeleteVeiculoCommand, bool>
    {
        public async Task<bool> HandleAsync(DeleteVeiculoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do veículo Id {Id}", command.Id);

                var veiculo = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (veiculo is null)
                {
                    logger.LogWarning("Tentativa de remover veículo inexistente. Id {Id}", command.Id);
                    return false;
                }

                // RN08 — veículo com rota associada não some: a rota guarda o histórico de
                // quilometragem da frota e ficaria apontando para um registro inexistente.
                if (await rotaRepository.ExisteComVeiculoAsync(currentUser.EmpresaId, veiculo.Id))
                    throw new InvalidOperationException(
                        "Não é possível excluir um veículo com rotas associadas. Encerre ou remova as rotas antes.");

                // Mesma regra para abastecimento: a FK é Restrict, então sem esta guarda o
                // banco recusaria a exclusão e o usuário veria um 500 em vez da explicação.
                if (await abastecimentoRepository.ExisteComVeiculoAsync(currentUser.EmpresaId, veiculo.Id))
                    throw new InvalidOperationException(
                        "Não é possível excluir um veículo com abastecimentos lançados. Remova os lançamentos antes.");

                await repository.DeleteAsync(veiculo);

                logger.LogInformation("Veículo removido com sucesso. Id {Id}", command.Id);

                // A descrição carrega placa e modelo porque o registro deixou de existir:
                // depois disso o id sozinho não identifica mais nada.
                await auditoria.RegistrarAsync(EntidadesAuditadas.Veiculo, AcoesAuditoria.Excluiu, command.Id,
                    $"Excluiu o veículo {veiculo.Placa} ({veiculo.MarcaVeiculo} {veiculo.NomeVeiculo})");

                return true;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao remover veículo Id {Id}", command.Id);
                throw;
            }
        }
    }
}
