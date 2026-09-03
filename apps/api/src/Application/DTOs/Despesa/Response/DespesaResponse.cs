namespace Frota360.Application.DTOs.Despesa.Response
{
    /// <summary>
    /// Já vem desnormalizada: veículo, tipo e motorista, como nas demais respostas de
    /// lançamento. Não há campo derivado na leitura.
    /// </summary>
    public class DespesaResponse
    {
        public int Id { get; set; }
        public int VeiculoId { get; set; }
        public string VeiculoNome { get; set; } = string.Empty;
        public string VeiculoPlaca { get; set; } = string.Empty;
        public int TipoDespesaId { get; set; }
        public string TipoDespesaNome { get; set; } = string.Empty;

        /// <summary>Nulo quando a despesa não é de ninguém em particular (IPVA, seguro).</summary>
        public int? MotoristaId { get; set; }

        public string? MotoristaNome { get; set; }

        public decimal Valor { get; set; }
        public DateTime DataDespesa { get; set; }
        public string? Observacao { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
