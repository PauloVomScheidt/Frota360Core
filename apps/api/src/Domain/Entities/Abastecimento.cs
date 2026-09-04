namespace Frota360.Domain.Entities
{
    /// <summary>
    /// Um abastecimento de um veículo da frota. É um apontamento fiscal: além de quanto
    /// custou, registra o que foi abastecido (combustível e litros), onde (posto
    /// credenciado), a que preço, com qual odômetro e sob qual nota — o que permite apurar
    /// R$/litro, km/litro e gasto por posto. A rota entra como contexto opcional, porque
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

        public int TipoCombustivelId { get; set; }

        /// <summary>Posto credenciado onde o abastecimento foi feito.</summary>
        public int PostoId { get; set; }

        /// <summary>Volume abastecido. Três casas porque é o que a bomba mostra.</summary>
        public decimal Litros { get; set; }

        /// <summary>Preço do litro na bomba. Três casas: R$ 6,199 é preço comum.</summary>
        public decimal ValorLitro { get; set; }

        /// <summary>
        /// Quanto foi pago, em reais. <b>Derivado</b> de <see cref="Litros"/> ×
        /// <see cref="ValorLitro"/> e recalculado no servidor a cada escrita — nunca vem
        /// do cliente. É o valor que a tela de custos soma.
        /// </summary>
        public decimal Valor { get; set; }

        /// <summary>
        /// Quilometragem do veículo no momento do abastecimento. Avança a ficha do
        /// veículo quando é maior que a atual — nunca a retrocede.
        /// </summary>
        public int Odometro { get; set; }

        public string NotaFiscal { get; set; } = string.Empty;

        /// <summary>Opcional: em autoatendimento não há frentista.</summary>
        public string? Frentista { get; set; }

        public DateTime DataAbastecimento { get; set; }
        public string? Observacao { get; set; }
        public DateTime DataInclusao { get; set; }

        // Navegação
        public Veiculo? Veiculo { get; set; }
        public Rota? Rota { get; set; }
        public Usuario? Motorista { get; set; }
        public Usuario? Usuario { get; set; }
        public TipoCombustivel? TipoCombustivel { get; set; }
        public Posto? Posto { get; set; }
    }
}
