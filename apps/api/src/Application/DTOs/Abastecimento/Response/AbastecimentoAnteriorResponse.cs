namespace Frota360.Application.DTOs.Abastecimento.Response
{
    /// <summary>
    /// A referência da estimativa de km/l: o abastecimento de maior odômetro abaixo do que está
    /// sendo digitado, naquele veículo.
    ///
    /// ⚠️ Carrega <b>só data e odômetro</b>, de propósito. A consulta enxerga o histórico do
    /// veículo inteiro — o consumo é propriedade do caminhão, não de quem dirigiu —, e é isso que
    /// corrige o km/l inflado que o motorista via quando o abastecimento anterior daquele veículo
    /// tinha sido lançado por outra pessoa. Devolver valor, litros ou o nome de quem abasteceu
    /// transformaria a correção num vazamento de gasto alheio.
    /// </summary>
    public class AbastecimentoAnteriorResponse
    {
        public DateTime DataAbastecimento { get; set; }
        public int Odometro { get; set; }
    }
}
