using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Veiculos;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Veiculos.Commands.CreateVeiculo
{
    public sealed class CreateVeiculoHandler(IVeiculoRepository repository,
                                             ICurrentUserService currentUser,
                                             IAuditoriaService auditoria,
                                             ILogger<CreateVeiculoHandler> logger)
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
                    // RN09 — a placa é gravada sempre em maiúsculas; o validator aceita as duas
                    // caixas para que um cliente da API não leve 422 por causa disso.
                    Placa = request.Placa.Trim().ToUpperInvariant(),
                    Quilometragem = request.Quilometragem,
                    UltimoMotorista = request.UltimoMotorista,
                    DataUltimaViagem = request.DataUltimaViagem,
                    DataInclusao = DateTime.Now
                };

                var criado = await repository.AddAsync(veiculo);

                logger.LogInformation("Veículo cadastrado com sucesso. Id: {Id} | Placa: {Placa}", criado.Id, criado.Placa);

                await auditoria.RegistrarAsync(EntidadesAuditadas.Veiculo, AcoesAuditoria.Criou, criado.Id,
                    $"Cadastrou o veículo {criado.Placa} ({criado.MarcaVeiculo} {criado.NomeVeiculo})");

                // Veículo recém-cadastrado não tem rota: não há o que consultar.
                return criado.ToResponse(emRota: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao cadastrar veículo com placa {Placa}", request.Placa);
                throw;
            }
        }
    }
}
