using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Abastecimentos.Commands.CreateAbastecimento
{
    public sealed class CreateAbastecimentoHandler(IAbastecimentoRepository repository,
                                                   IVeiculoRepository veiculoRepository,
                                                   IUsuarioRepository usuarioRepository,
                                                   IRotaRepository rotaRepository,
                                                   ITipoCombustivelRepository tipoCombustivelRepository,
                                                   IPostoRepository postoRepository,
                                                   ICurrentUserService currentUser,
                                                   IAuditoriaService auditoria,
                                                   ILogger<CreateAbastecimentoHandler> logger)
        : ICommandHandler<CreateAbastecimentoCommand, AbastecimentoResponse>
    {
        public async Task<AbastecimentoResponse> HandleAsync(CreateAbastecimentoCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;

            try
            {
                logger.LogInformation("Iniciando lançamento de abastecimento do veículo {VeiculoId}", request.VeiculoId);

                // Escopado pela empresa: id de outra empresa simplesmente "não existe".
                var veiculo = await veiculoRepository.GetByIdAsync(request.VeiculoId, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException($"Veículo {request.VeiculoId} não encontrado.");

                var motorista = await ResolverMotoristaAsync(request.MotoristaId);
                var rota = await ResolverRotaAsync(motorista.Id, veiculo.Id);
                var tipoCombustivel = await ResolverTipoCombustivelAsync(request.TipoCombustivelId);
                var posto = await ResolverPostoAsync(request.PostoId);

                var valor = AbastecimentoMappings.CalcularValor(request.Litros, request.ValorLitro);

                var abastecimento = await repository.AddAsync(new Abastecimento
                {
                    EmpresaId = currentUser.EmpresaId,
                    VeiculoId = veiculo.Id,
                    RotaId = rota?.Id,
                    MotoristaId = motorista.Id,
                    // Quem digitou. A gestão lança pelo motorista; o registro continua sendo dela.
                    UsuarioId = currentUser.UsuarioId,
                    TipoCombustivelId = tipoCombustivel.Id,
                    PostoId = posto.Id,
                    Litros = request.Litros,
                    ValorLitro = request.ValorLitro,
                    Valor = valor,
                    Odometro = request.Odometro,
                    NotaFiscal = request.NotaFiscal.Trim(),
                    Frentista = string.IsNullOrWhiteSpace(request.Frentista) ? null : request.Frentista.Trim(),
                    DataAbastecimento = request.DataAbastecimento,
                    Observacao = request.Observacao,
                    DataInclusao = DateTime.Now
                });

                var odometroAnterior = await AvancarQuilometragemDoVeiculoAsync(veiculo, request.Odometro);

                logger.LogInformation("Abastecimento lançado com sucesso. Id {Id} | Veículo {VeiculoId} | {Litros} L | R$ {Valor}",
                    abastecimento.Id, veiculo.Id, request.Litros, valor);

                var alteracoes = new AlteracoesBuilder()
                    .Comparar($"Odômetro do veículo {veiculo.Placa}", odometroAnterior, odometroAnterior is null ? null : request.Odometro)
                    .Construir();

                await auditoria.RegistrarAsync(EntidadesAuditadas.Abastecimento, AcoesAuditoria.Criou, abastecimento.Id,
                    $"Lançou {request.Litros:0.###} L de {tipoCombustivel.Nome} no veículo {veiculo.Placa} " +
                    $"em {posto.Nome} por R$ {valor:0.00} para {motorista.Nome}", alteracoes);

                // Recarrega em vez de montar as navegações à mão: a resposta precisa do
                // veículo, do motorista, de quem lançou, da rota, do combustível e do posto.
                // Uma consulta a mais paga a clareza.
                var criado = await repository.GetByIdAsync(abastecimento.Id, currentUser.EmpresaId);

                return criado!.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao lançar abastecimento do veículo {VeiculoId}", request.VeiculoId);
                throw;
            }
        }

        private async Task<Usuario> ResolverMotoristaAsync(int? motoristaIdDoCorpo)
        {
            if (currentUser.EhMotorista())
            {
                return await usuarioRepository.GetMotoristaByIdAsync(currentUser.UsuarioId, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException("Motorista não encontrado.");
            }

            if (motoristaIdDoCorpo is not int id)
                throw new InvalidOperationException("Informe o motorista do abastecimento.");

            return await usuarioRepository.GetMotoristaByIdAsync(id, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Motorista {id} não encontrado.");
        }

        private async Task<Rota?> ResolverRotaAsync(int motoristaId, int veiculoId)
        {
            // Uma linha, não o histórico inteiro do motorista para filtrar no cliente.
            var aberta = await rotaRepository.GetRotaAbertaDoMotoristaAsync(currentUser.EmpresaId, motoristaId);

            if (aberta is null)
                return null;

            if (currentUser.EhMotorista() && aberta.CodigoVeiculo != veiculoId)
                throw new InvalidOperationException(
                    "Você está em rota com outro veículo. Lance o abastecimento no veículo da sua rota aberta.");

            return aberta.CodigoVeiculo == veiculoId ? aberta : null;
        }

        private async Task<TipoCombustivel> ResolverTipoCombustivelAsync(int tipoCombustivelId)
        {
            var tipo = await tipoCombustivelRepository.GetByIdAsync(tipoCombustivelId, currentUser.EmpresaId)
                ?? throw new InvalidOperationException($"Tipo de combustível {tipoCombustivelId} não encontrado.");

            // Item aposentado do catálogo continua nomeando o passado, mas não recebe
            // lançamento novo — é o que "inativar" significa.
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
        /// Terceiro caminho que move o odômetro, ao lado de abrir/encerrar rota e concluir
        /// manutenção — e, como eles, só para frente: um lançamento retroativo não reescreve
        /// a ficha com número velho.
        /// </summary>
        /// <returns>O odômetro anterior quando ele avançou; nulo quando não houve avanço.</returns>
        private async Task<int?> AvancarQuilometragemDoVeiculoAsync(Veiculo veiculo, int odometro)
        {
            if (odometro <= veiculo.Quilometragem)
                return null;

            var anterior = veiculo.Quilometragem;
            veiculo.Quilometragem = odometro;
            await veiculoRepository.UpdateAsync(veiculo);

            logger.LogInformation("Quilometragem do veículo {VeiculoId} atualizada de {Anterior} para {Atual} pelo abastecimento",
                veiculo.Id, anterior, odometro);

            return anterior;
        }
    }
}
