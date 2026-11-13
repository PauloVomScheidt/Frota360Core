namespace Frota360.Domain.ReadModels
{
    /// <summary>
    /// Quilometragem apurada de um veículo no período, somada de <c>Rota.KmPercorrido</c> das
    /// rotas <b>encerradas</b> — é o denominador do custo por km.
    ///
    /// Rota ainda aberta não tem <c>KmPercorrido</c>, então o período corrente subestima o km
    /// e, por consequência, superestima o R$/km.
    /// </summary>
    public sealed record KmPorVeiculo(
        int VeiculoId,
        string VeiculoNome,
        string VeiculoPlaca,
        int Km,
        int Rotas);
}
