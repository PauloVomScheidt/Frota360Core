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

                var abastecimento = await repository.AddAsync(new Abastecimento
                {
                    EmpresaId = currentUser.EmpresaId,
                    VeiculoId = veiculo.Id,
                    RotaId = rota?.Id,
                    MotoristaId = motorista.Id,
                    // Quem digitou. A gestão lança pelo motorista; o registro continua sendo dela.
                    UsuarioId = currentUser.UsuarioId,
                    Valor = request.Valor,
                    DataAbastecimento = request.DataAbastecimento,
                    Observacao = request.Observacao,
                    DataInclusao = DateTime.UtcNow
                });

                logger.LogInformation("Abastecimento lançado com sucesso. Id {Id} | Veículo {VeiculoId} | Motorista {MotoristaId} | R$ {Valor}",
                    abastecimento.Id, veiculo.Id, motorista.Id, request.Valor);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Abastecimento, AcoesAuditoria.Criou, abastecimento.Id,
                    $"Lançou abastecimento de R$ {request.Valor:0.00} no veículo {veiculo.Placa} para {motorista.Nome}");

                // Recarrega em vez de montar as navegações à mão: a resposta precisa do
                // veículo, do motorista, de quem lançou e da rota. Uma consulta a mais paga
                // a clareza.
                var criado = await repository.GetByIdAsync(abastecimento.Id, currentUser.EmpresaId);

                return criado!.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao lançar abastecimento do veículo {VeiculoId}", request.VeiculoId);
                throw;
            }
        }

        /// <summary>
        /// De quem é o gasto. Para o motorista o id do corpo é ignorado — ele lança sempre
        /// em si mesmo, como no <c>CreateRotaHandler</c>: o cliente não escolhe de quem é o
        /// registro. Para a gestão o motorista é obrigatório e resolvido por
        /// <c>GetMotoristaByIdAsync</c>, que já filtra empresa <b>e</b> role.
        /// </summary>
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

        /// <summary>
        /// A rota é contexto e é sempre derivada: liga o abastecimento à rota aberta do
        /// motorista naquele veículo, quando houver. Não há rota "ativa" como estado — é
        /// <c>Ativo &amp;&amp; DataFim is null</c>, a mesma definição que a tela usa.
        ///
        /// A trava de veículo vale só para quem está dirigindo: o motorista em rota aberta
        /// abastece o carro da rota. A gestão pode lançar por ele em qualquer veículo —
        /// troca, apoio, abastecimento de outro carro no mesmo dia.
        /// </summary>
        private async Task<Rota?> ResolverRotaAsync(int motoristaId, int veiculoId)
        {
            var rotas = await rotaRepository.GetAllByMotoristaAsync(currentUser.EmpresaId, motoristaId);
            var aberta = rotas.FirstOrDefault(r => r.Ativo && r.DataFim is null);

            if (aberta is null)
                return null;

            if (currentUser.EhMotorista() && aberta.CodigoVeiculo != veiculoId)
                throw new InvalidOperationException(
                    "Você está em rota com outro veículo. Lance o abastecimento no veículo da sua rota aberta.");

            return aberta.CodigoVeiculo == veiculoId ? aberta : null;
        }
    }
}
