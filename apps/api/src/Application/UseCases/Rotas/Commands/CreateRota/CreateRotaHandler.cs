using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.UseCases.Rotas;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Commands.CreateRota
{
    public sealed class CreateRotaHandler(IRotaRepository repository, ILogger<CreateRotaHandler> logger)
        : ICommandHandler<CreateRotaCommand, RotaResponse>
    {
        public async Task<RotaResponse> HandleAsync(CreateRotaCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;

            try
            {
                logger.LogInformation("Iniciando cadastro da rota {Origem} -> {Destino}", request.Origem, request.Destino);

                var rota = new Rota
                {
                    Origem = request.Origem,
                    Destino = request.Destino,
                    CodigoMotorista = request.CodigoMotorista,
                    CodigoVeiculo = request.CodigoVeiculo,
                    Ativo = request.Ativo,
                    DataInicio = request.DataInicio,
                    DataFim = request.DataFim,
                    DataInclusao = DateTime.UtcNow,
                };

                var criado = await repository.AddAsync(rota);

                logger.LogInformation("Rota cadastrada com sucesso. Id {Id} | Origem {Origem} | Destino {Destino}", criado.Id, criado.Origem, criado.Destino);

                return criado.ToResponse();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao cadastrar rota {Origem} -> {Destino}", request.Origem, request.Destino);
                throw;
            }
        }
    }
}
