using Frota360.Application.DTOs.Rota.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.Rotas
{
    /// <summary>Mapeamento centralizado de <see cref="Rota"/> para <see cref="RotaResponse"/>.</summary>
    public static class RotaMappings
    {
        public static RotaResponse ToResponse(this Rota r) => new()
        {
            Id = r.Id,
            Ativo = r.Ativo,
            CodigoMotorista = r.CodigoMotorista,
            NomeMotorista = r.Motorista?.Nome,
            CodigoVeiculo = r.CodigoVeiculo,
            DataFim = r.DataFim,
            DataInicio = r.DataInicio,
            Destino = r.Destino,
            Origem = r.Origem,
            DataInclusao = r.DataInclusao,
            KmInicial = r.KmInicial,
            KmFinal = r.KmFinal,
            KmPercorrido = r.KmPercorrido
        };
    }
}
