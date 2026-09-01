using Frota360.Domain.Enums;

namespace Frota360.Domain.ReadModels
{
    /// <summary>
    /// Soma de um veículo em <b>uma</b> origem. O handler pivota as linhas de origens
    /// diferentes do mesmo veículo numa linha só da resposta.
    /// </summary>
    public sealed record TotalCustoPorVeiculo(
        int VeiculoId,
        string VeiculoNome,
        string VeiculoPlaca,
        OrigemCusto Origem,
        decimal Total,
        int Quantidade);
}
