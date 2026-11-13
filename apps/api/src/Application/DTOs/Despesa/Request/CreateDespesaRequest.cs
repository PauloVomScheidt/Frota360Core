namespace Frota360.Application.DTOs.Despesa.Request
{
    /// <summary>
    /// Lançamento de um custo avulso. Só a gestão lança, então não há aqui o par
    /// motorista/usuário do abastecimento: o autor fica na trilha de auditoria.
    /// </summary>
    public class CreateDespesaRequest
    {
        public int VeiculoId { get; set; }

        public int TipoDespesaId { get; set; }

        /// <summary>
        /// Opcional — multa tem dono, IPVA não. Quando informado, é resolvido por
        /// <c>GetMotoristaByIdAsync</c>, que filtra empresa e role.
        /// </summary>
        public int? MotoristaId { get; set; }

        public decimal Valor { get; set; }

        public DateTime DataDespesa { get; set; }

        public string? Observacao { get; set; }
    }
}
