using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Abastecimentos.Commands.UpdateAbastecimento
{
    public sealed class UpdateAbastecimentoHandler(IAbastecimentoRepository repository,
                                                   IVeiculoRepository veiculoRepository,
                                                   ITipoCombustivelRepository tipoCombustivelRepository,
                                                   IPostoRepository postoRepository,
                                                   ICurrentUserService currentUser,
                                                   IAuditoriaService auditoria,
                                                   ILogger<UpdateAbastecimentoHandler> logger)
        : ICommandHandler<UpdateAbastecimentoCommand, AbastecimentoResponse?>
    {
        public async Task<AbastecimentoResponse?> HandleAsync(UpdateAbastecimentoCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Iniciando correção do abastecimento Id {Id}", command.Id);

                var abastecimento = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                if (abastecimento is null)
                {
                    logger.LogWarning("Tentativa de corrigir abastecimento inexistente. Id {Id}", command.Id);
                    return null;
                }

                // Lançamento de outro motorista responde 404, não 403: para quem não é o dono
                // do gasto, ele simplesmente não existe — mesma regra da rota alheia.
                if (currentUser.EhMotorista() && abastecimento.MotoristaId != currentUser.UsuarioId)
                {
                    logger.LogWarning("Motorista {UsuarioId} tentou corrigir o abastecimento {Id}, que não é dele",
                        currentUser.UsuarioId, command.Id);
                    return null;
                }

                var request = command.Data;
                var tipoCombustivel = await ResolverTipoCombustivelAsync(request.TipoCombustivelId);
                var posto = await ResolverPostoAsync(request.PostoId);
                var valor = AbastecimentoMappings.CalcularValor(request.Litros, request.ValorLitro);
                var notaFiscal = request.NotaFiscal.Trim();
                var frentista = string.IsNullOrWhiteSpace(request.Frentista) ? null : request.Frentista.Trim();

                var alteracoes = new AlteracoesBuilder()
                    .Comparar("Combustível", abastecimento.TipoCombustivel?.Nome, tipoCombustivel.Nome)
                    .Comparar("Posto", abastecimento.Posto?.Nome, posto.Nome)
                    .Comparar("Litros", abastecimento.Litros, request.Litros)
                    .Comparar("Valor do litro", abastecimento.ValorLitro, request.ValorLitro)
                    .Comparar("Valor", abastecimento.Valor, valor)
                    .Comparar("Odômetro", abastecimento.Odometro, request.Odometro)
                    .Comparar("Nota fiscal", abastecimento.NotaFiscal, notaFiscal)
                    .Comparar("Frentista", abastecimento.Frentista, frentista)
                    .Comparar("Data", abastecimento.DataAbastecimento, request.DataAbastecimento)
                    .Comparar("Observação", abastecimento.Observacao, request.Observacao)
                    .Construir();

                // Veículo, motorista e rota ficam de fora do PUT: trocar qualquer um reescreveria
                // a atribuição do gasto (ver UpdateAbastecimentoRequest).
                abastecimento.TipoCombustivelId = tipoCombustivel.Id;
                abastecimento.PostoId = posto.Id;
                abastecimento.Litros = request.Litros;
                abastecimento.ValorLitro = request.ValorLitro;
                abastecimento.Valor = valor;
                abastecimento.Odometro = request.Odometro;
                abastecimento.NotaFiscal = notaFiscal;
                abastecimento.Frentista = frentista;
                abastecimento.DataAbastecimento = request.DataAbastecimento;
                abastecimento.Observacao = request.Observacao;

                await repository.UpdateAsync(abastecimento);

                await AvancarQuilometragemDoVeiculoAsync(abastecimento.VeiculoId, request.Odometro);

                logger.LogInformation("Abastecimento corrigido com sucesso. Id {Id}", command.Id);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Abastecimento, AcoesAuditoria.Atualizou, command.Id,
                    $"Corrigiu o abastecimento #{command.Id} do veículo {abastecimento.Veiculo?.Placa}", alteracoes);

                var atualizado = await repository.GetByIdAsync(command.Id, currentUser.EmpresaId);

                return atualizado!.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao corrigir abastecimento Id {Id}", command.Id);
                throw;
            }
        }

        private async Task<TipoCombustivel> ResolverTipoCombustivelAsync(int tipoCombustivelId)
        {
            var tipo = await tipoCombustivelRepository.GetByIdAsync(tipoCombustivelId, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Tipo de combustível {tipoCombustivelId} não encontrado.");

            if (!tipo.Ativo)
                throw new InvalidOperationException($"O combustível \"{tipo.Nome}\" está inativo.");

            return tipo;
        }

        private async Task<Posto> ResolverPostoAsync(int postoId)
        {
            var posto = await postoRepository.GetByIdAsync(postoId, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Posto {postoId} não encontrado.");

            if (!posto.Ativo)
                throw new InvalidOperationException($"O posto \"{posto.Nome}\" está descredenciado.");

            return posto;
        }

        /// <summary>
        /// Mesma política do lançamento: corrigir o odômetro para cima avança a ficha do
        /// veículo, corrigir para baixo não a retrocede — o odômetro nunca anda para trás,
        /// nem aqui nem na exclusão.
        /// </summary>
        private async Task AvancarQuilometragemDoVeiculoAsync(int veiculoId, int odometro)
        {
            var veiculo = await veiculoRepository.GetByIdAsync(veiculoId, currentUser.EmpresaId);

            if (veiculo is null || odometro <= veiculo.Quilometragem)
                return;

            var anterior = veiculo.Quilometragem;
            veiculo.Quilometragem = odometro;
            await veiculoRepository.UpdateAsync(veiculo);

            logger.LogInformation("Quilometragem do veículo {VeiculoId} atualizada de {Anterior} para {Atual} pela correção do abastecimento",
                veiculoId, anterior, odometro);
        }
    }
}
