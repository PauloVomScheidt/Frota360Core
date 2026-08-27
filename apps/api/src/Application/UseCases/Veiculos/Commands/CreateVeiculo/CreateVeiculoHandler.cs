using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Veiculos;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Veiculos.Commands.CreateVeiculo
{
    public sealed class CreateVeiculoHandler(IVeiculoRepository repository, ICurrentUserService currentUser, ILogger<CreateVeiculoHandler> logger)
        : ICommandHandler<CreateVeiculoCommand, VeiculoResponse>
    {
        public async Task<VeiculoResponse> HandleAsync(CreateVeiculoCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;

            try
            {
                logger.LogInformation("Iniciando cadastro de veículo com placa {Placa}", request.Placa);

                var veiculo = new Veiculo
                {
                    EmpresaId = currentUser.EmpresaId,
                    NomeVeiculo = request.NomeVeiculo,
                    MarcaVeiculo = request.MarcaVeiculo,
                    Placa = request.Placa,
                    Quilometragem = request.Quilometragem,
                    UltimoMotorista = request.UltimoMotorista,
                    DataUltimaViagem = request.DataUltimaViagem,
                    DataInclusao = DateTime.UtcNow
                };

                var criado = await repository.AddAsync(veiculo);

                logger.LogInformation("Veículo cadastrado com sucesso. Id: {Id} | Placa: {Placa}", criado.Id, criado.Placa);

                return criado.ToResponse();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao cadastrar veículo com placa {Placa}", request.Placa);
                throw;
            }
        }
    }
}
