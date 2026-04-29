using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;

namespace Frota360.Application.Services
{
    public class VeiculoService(IVeiculoRepository repository) : IVeiculoService
    {
        public async Task<IEnumerable<VeiculoResponse>> GetAllAsync()
        {
            var veiculos = await repository.GetAllAsync();
            return veiculos.Select(ToResponse);
        }

        public async Task<VeiculoResponse> AddAsync(CreateVeiculoRequest request)
        {
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
            return ToResponse(criado);
        }

        public async Task<VeiculoResponse?> UpdateAsync(int id, UpdateVeiculoRequest request)
        {
            var veiculo = await repository.GetByIdAsync(id);

            if (veiculo is null)
                return null;

            veiculo.NomeVeiculo = request.NomeVeiculo;
            veiculo.MarcaVeiculo = request.MarcaVeiculo;
            veiculo.Placa = request.Placa;
            veiculo.Quilometragem = request.Quilometragem;
            veiculo.UltimoMotorista = request.UltimoMotorista;
            veiculo.DataUltimaViagem = request.DataUltimaViagem;

            var atualizado = await repository.UpdateAsync(veiculo);
            return ToResponse(atualizado);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var veiculo = await repository.GetByIdAsync(id);

            if (veiculo is null)
                return false;

            await repository.DeleteAsync(veiculo);
            return true;
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
