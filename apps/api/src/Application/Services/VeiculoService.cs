using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.Services
{
    public class VeiculoService(IVeiculoRepository repository, ILogger<VeiculoService> logger) : IVeiculoService
    {
        public async Task<IEnumerable<VeiculoResponse>> GetAllAsync()
        {
            try
            {
                logger.LogInformation("Buscando todos os veículos");

                var veiculos = await repository.GetAllAsync();

                logger.LogInformation("Foram encontrados {QuantidadeVeiculos} veículos", veiculos.Count());

                return veiculos.Select(ToResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao buscar todos os veículos");
                throw;
            }
        }

        public async Task<VeiculoResponse> AddAsync(CreateVeiculoRequest request)
        {
            try
            {
                logger.LogInformation("Iniciando cadastro de veículo com placa {Placa}", request.Placa);

                var veiculo = new Veiculo
                {
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

                return ToResponse(criado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao cadastrar veículo com placa {Placa}", request.Placa);
                throw;
            }
        }

        public async Task<VeiculoResponse?> UpdateAsync(int id, UpdateVeiculoRequest request)
        {
            try
            {
                logger.LogInformation("Iniciando atualização do veículo Id {Id}", id);

                var veiculo = await repository.GetByIdAsync(id);

                if (veiculo is null)
                {
                    logger.LogWarning("Tentativa de atualizar veículo inexistente. Id {Id}", id);
                    return null;
                }

                veiculo.NomeVeiculo = request.NomeVeiculo;
                veiculo.MarcaVeiculo = request.MarcaVeiculo;
                veiculo.Placa = request.Placa;
                veiculo.Quilometragem = request.Quilometragem;
                veiculo.UltimoMotorista = request.UltimoMotorista;
                veiculo.DataUltimaViagem = request.DataUltimaViagem;

                var atualizado = await repository.UpdateAsync(veiculo);

                logger.LogInformation("Veículo atualizado com sucesso. Id {Id}", atualizado.Id);

                return ToResponse(atualizado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao atualizar veículo Id {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do veículo Id {Id}", id);

                var veiculo = await repository.GetByIdAsync(id);

                if (veiculo is null)
                {
                    logger.LogWarning("Tentativa de remover veículo inexistente. Id {Id}", id);
                    return false;
                }

                await repository.DeleteAsync(veiculo);

                logger.LogInformation("Veículo removido com sucesso. Id {Id}", id);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover veículo Id {Id}", id);
                throw;
            }
        }

        // mapeamento centralizado
        private static VeiculoResponse ToResponse(Veiculo v) => new()
        {
            Id = v.Id,
            NomeVeiculo = v.NomeVeiculo,
            MarcaVeiculo = v.MarcaVeiculo,
            Placa = v.Placa,
            Quilometragem = v.Quilometragem,
            UltimoMotorista = v.UltimoMotorista,
            DataUltimaViagem = v.DataUltimaViagem,
            DataInclusao = v.DataInclusao
        };
    }
}
