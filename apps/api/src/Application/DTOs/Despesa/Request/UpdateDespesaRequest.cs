namespace Frota360.Application.DTOs.Despesa.Request
{
    /// <summary>
    /// A correção alcança <b>todos</b> os campos, inclusive veículo, tipo e motorista —
    /// ao contrário do abastecimento, onde só valor, data e observação são editáveis.
    ///
    /// Lá a trava existe porque trocar o veículo ou o motorista reatribuiria um gasto
    /// sujeito a recorte por dono; aqui não há recorte, e a auditoria grava o diff campo
    /// a campo. Exigir "exclua e lance de novo" para corrigir um IPVA digitado no veículo
    /// errado seria atrito sem regra por trás.
    /// </summary>
    public class UpdateDespesaRequest
    {
        public int VeiculoId { get; set; }
        public int TipoDespesaId { get; set; }
        public int? MotoristaId { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataDespesa { get; set; }
        public string? Observacao { get; set; }
    }
}
