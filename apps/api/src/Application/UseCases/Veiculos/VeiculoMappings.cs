using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.Veiculos
{
    /// <summary>Mapeamento centralizado de <see cref="Veiculo"/> para <see cref="VeiculoResponse"/>.</summary>
    public static class VeiculoMappings
    {
        public static VeiculoResponse ToResponse(this Veiculo v) => new()
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
