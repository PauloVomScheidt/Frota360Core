namespace Frota360.Domain.Entities
{
    /// <summary>
    /// Um custo da frota que não tem tela própria: pedágio, multa, IPVA, seguro,
    /// licenciamento. É a <b>terceira origem</b> de custo do sistema, ao lado do
    /// abastecimento e da manutenção concluída.
    ///
    /// Diferente daquelas duas, aqui a tabela é <b>fonte de verdade</b> e não espelho —
    /// não existe outro lugar onde este gasto seja registrado.
    /// </summary>
    public class Despesa
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }

        /// <summary>
        /// Obrigatório. IPVA, seguro e licenciamento já são por veículo na prática, e
        /// exigi-lo mantém o resumo por veículo fechando com os totais da tela de custos.
        /// </summary>
        public int VeiculoId { get; set; }

        public int TipoDespesaId { get; set; }

        /// <summary>
        /// De quem é o gasto, quando é de alguém: multa tem dono, IPVA não. É o que faz o
        /// filtro por motorista da tela de custos alcançar a despesa.
        ///
        /// Não há o par <c>UsuarioId</c> ("quem lançou") do abastecimento: lá ele existe
        /// porque o recorte do motorista depende de separar dono de digitador, e aqui só a
        /// gestão lança. O autor fica na trilha de auditoria.
        /// </summary>
        public int? MotoristaId { get; set; }

        /// <summary>Quanto foi pago, em reais.</summary>
        public decimal Valor { get; set; }

        public DateTime DataDespesa { get; set; }
        public string? Observacao { get; set; }
        public DateTime DataInclusao { get; set; }

        // Navegação
        public Veiculo? Veiculo { get; set; }
        public TipoDespesa? Tipo { get; set; }
        public Usuario? Motorista { get; set; }
    }
}
