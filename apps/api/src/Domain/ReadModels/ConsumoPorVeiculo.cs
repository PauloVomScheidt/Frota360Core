namespace Frota360.Domain.ReadModels
{
    /// <summary>
    /// Consumo apurado de um veículo no período, a partir dos abastecimentos: a distância
    /// que eles cobrem e os litros que a pagaram.
    ///
    /// <para>
    /// O <c>Km</c> vem do <b>odômetro do abastecimento</b>, não de <c>Rota.KmPercorrido</c>
    /// como em <see cref="KmPorVeiculo"/>: combustível é queimado dentro e fora de rota, e
    /// usar o km das rotas encerradas subestimaria o consumo. São dois "km" diferentes na
    /// mesma tela de propósito — cada KPI diz de onde o seu vem.
    /// </para>
    ///
    /// <para>
    /// <c>Litros</c> já vem <b>sem os do primeiro abastecimento</b> do período: eles pagaram
    /// o trecho anterior a ele, e mantê-los infla o denominador e subestima o km/l de forma
    /// sistemática. Com menos de dois abastecimentos não há intervalo, e o consumo não existe.
    /// </para>
    /// </summary>
    public sealed record ConsumoPorVeiculo(
        int VeiculoId,
        string VeiculoNome,
        string VeiculoPlaca,
        decimal Litros,
        int Km,
        int Abastecimentos);
}
