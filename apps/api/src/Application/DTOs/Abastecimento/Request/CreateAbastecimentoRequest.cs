namespace Frota360.Application.DTOs.Abastecimento.Request
{
    /// <summary>
    /// O apontamento fiscal do abastecimento. Duas coisas <b>não</b> entram no corpo: a
    /// rota, que a API deriva da rota aberta do motorista naquele veículo, e o valor total,
    /// que é recalculado a partir de litros × valor do litro.
    /// </summary>
    public class CreateAbastecimentoRequest
    {
        public int VeiculoId { get; set; }

        /// <summary>
        /// De quem é o gasto. Obrigatório para a gestão, que lança em nome do motorista.
        /// Para a role Motorista a API <b>ignora</b> o que vier aqui e usa o próprio usuário
        /// do token — ninguém lança abastecimento na conta de outro.
        /// </summary>
        public int? MotoristaId { get; set; }

        public int TipoCombustivelId { get; set; }

        /// <summary>Posto credenciado. Item inativo do catálogo é recusado com 422.</summary>
        public int PostoId { get; set; }

        public decimal Litros { get; set; }
        public decimal ValorLitro { get; set; }

        /// <summary>
        /// Quilometragem no momento do abastecimento. Avança a ficha do veículo quando é
        /// maior que a atual; menor, é aceito e não retrocede nada.
        /// </summary>
        public int Odometro { get; set; }

        public string NotaFiscal { get; set; } = string.Empty;
        public string? Frentista { get; set; }

        public DateTime DataAbastecimento { get; set; }
        public string? Observacao { get; set; }
    }
}
