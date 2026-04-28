using Frota360.Application.DTOs.Veiculo;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;

namespace Frota360.Application.Services
{
    public class VeiculoService(IVeiculoRepository repository) : IVeiculoService
    {
        public async Task<IEnumerable<VeiculoResponse>> GetAllAsync()
        {
            var veiculos = await repository.GetAllAsync();

            return veiculos.Select(v => new VeiculoResponse
            {
                Id = v.Id,
                NomeVeiculo = v.NomeVeiculo,
                MarcaVeiculo = v.MarcaVeiculo,
                Placa = v.Placa,
                Quilometragem = v.Quilometragem,
                UltimoMotorista = v.UltimoMotorista,
                DataUltimaViagem = v.DataUltimaViagem,
                DataInclusao = v.DataInclusao
            });
        }
    }
}
