using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.Veiculos
{
    /// <summary>Mapeamento centralizado de <see cref="Veiculo"/> para <see cref="VeiculoResponse"/>.</summary>
    public static class VeiculoMappings
    {
        /// <summary>
        /// <paramref name="emRota"/> não tem default de propósito: o dado vive na tabela
        /// <c>Rota</c> e só o handler sabe consultá-lo. Um <c>false</c> implícito passaria
        /// despercebido e a tela mostraria "Disponível" para um carro na estrada.
        /// </summary>
        public static VeiculoResponse ToResponse(this Veiculo v, bool emRota) => new()
        {
            EmRota = emRota,
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
