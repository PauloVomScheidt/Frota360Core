namespace Frota360.Domain.Entities
{
    /// <summary>
    /// Um abastecimento de um veículo da frota. O apontamento é curto de propósito —
    /// veículo, motorista, valor, data e observação: é o que se consegue registrar no
    /// posto sem atrito, e é o suficiente para relatório de gasto por veículo, por
    /// motorista, por rota e por período. A rota entra como contexto opcional, porque
    /// abastecer no pátio ou fora de viagem é comum e inventar uma rota falsa para
    /// registrar isso seria pior.
    /// </summary>
    public class Abastecimento
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }

        public int VeiculoId { get; set; }

        /// <summary>
        /// Rota em que o abastecimento aconteceu, quando havia uma aberta do motorista
        /// naquele veículo. <b>Sempre derivada no servidor</b> — o cliente não a informa.
        /// </summary>
        public int? RotaId { get; set; }

        /// <summary>
        /// A quem o abastecimento pertence. É o segundo eixo de isolamento: o motorista
        /// enxerga e corrige o que é dele, como em <c>/minhas-rotas</c> — inclusive o que
        /// a gestão lançou <b>para</b> ele.
        /// </summary>
        public int MotoristaId { get; set; }

        /// <summary>
        /// Quem digitou. Separado do motorista porque a gestão lança em nome dele: o
        /// gasto é do motorista, o registro é de quem o fez.
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>Quanto foi pago, em reais.</summary>
        public decimal Valor { get; set; }

        public DateTime DataAbastecimento { get; set; }
        public string? Observacao { get; set; }
        public DateTime DataInclusao { get; set; }

        // Navegação
        public Veiculo? Veiculo { get; set; }
        public Rota? Rota { get; set; }
        public Usuario? Motorista { get; set; }
        public Usuario? Usuario { get; set; }
    }
}
