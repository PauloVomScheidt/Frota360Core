namespace Frota360.Application.DTOs.Abastecimento.Request
{
    /// <summary>
    /// O apontamento é curto de propósito: o que se consegue registrar no posto sem atrito.
    /// A rota <b>não</b> entra no corpo — a API a deriva da rota aberta do motorista naquele
    /// veículo, quando houver.
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

        public decimal Valor { get; set; }
        public DateTime DataAbastecimento { get; set; }
        public string? Observacao { get; set; }
    }
}
