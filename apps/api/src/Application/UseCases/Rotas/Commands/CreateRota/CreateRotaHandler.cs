using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Rotas;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Commands.CreateRota
{
    public sealed class CreateRotaHandler(IRotaRepository repository,
                                          IMotoristaRepository motoristaRepository,
                                          IVeiculoRepository veiculoRepository,
                                          ICurrentUserService currentUser,
                                          ILogger<CreateRotaHandler> logger)
        : ICommandHandler<CreateRotaCommand, RotaResponse>
    {
        public async Task<RotaResponse> HandleAsync(CreateRotaCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;

            try
            {
                logger.LogInformation("Iniciando cadastro da rota {Origem} -> {Destino}", request.Origem, request.Destino);

                // Buscas escopadas pela empresa do usuário: garantem que os ids vindos do corpo
                // não alcancem motoristas ou veículos de outra empresa.
                var motorista = await motoristaRepository.GetByIdAsync(request.CodigoMotorista, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException($"Motorista {request.CodigoMotorista} não encontrado.");

                var veiculo = await veiculoRepository.GetByIdAsync(request.CodigoVeiculo, currentUser.EmpresaId)
                    ?? throw new InvalidOperationException($"Veículo {request.CodigoVeiculo} não encontrado.");

                if (request.KmInicial < veiculo.Quilometragem)
                    throw new InvalidOperationException(
                        $"A quilometragem inicial não pode ser menor que o odômetro atual do veículo ({veiculo.Quilometragem} km).");

                // Veículo rodou fora do sistema: o número mais recente vence.
                if (request.KmInicial > veiculo.Quilometragem)
                {
                    var anterior = veiculo.Quilometragem;
                    veiculo.Quilometragem = request.KmInicial;
                    await veiculoRepository.UpdateAsync(veiculo);

                    logger.LogInformation("Quilometragem do veículo {VeiculoId} atualizada de {Anterior} para {Atual} pela abertura da rota",
                        veiculo.Id, anterior, request.KmInicial);
                }

                var rota = new Rota
                {
                    EmpresaId = currentUser.EmpresaId,
                    Origem = request.Origem,
                    Destino = request.Destino,
                    CodigoMotorista = motorista.Id,
                    CodigoVeiculo = veiculo.Id,
                    Ativo = true,
                    DataInicio = request.DataInicio,
                    DataFim = null,
                    KmInicial = request.KmInicial,
                    DataInclusao = DateTime.UtcNow,
                };

                var criado = await repository.AddAsync(rota);

                logger.LogInformation("Rota cadastrada com sucesso. Id {Id} | Origem {Origem} | Destino {Destino}", criado.Id, criado.Origem, criado.Destino);

                return criado.ToResponse();
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Erro ao cadastrar rota {Origem} -> {Destino}", request.Origem, request.Destino);
                throw;
            }
        }
    }
}
