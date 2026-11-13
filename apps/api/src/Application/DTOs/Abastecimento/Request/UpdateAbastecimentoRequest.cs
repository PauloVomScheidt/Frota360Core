namespace Frota360.Application.DTOs.Abastecimento.Request
{
    /// <summary>
    /// Correção de um lançamento — errar o que se digitou no posto é comum. Veículo e
    /// motorista não entram: trocar qualquer um dos dois reescreveria a atribuição do gasto,
    /// e o vínculo com a rota foi resolvido no momento do lançamento. Nesse caso, exclua e
    /// lance de novo. O valor total continua fora do corpo — ele é derivado.
    /// </summary>
    public class UpdateAbastecimentoRequest
    {
        public int TipoCombustivelId { get; set; }
        public int PostoId { get; set; }
        public decimal Litros { get; set; }
        public decimal ValorLitro { get; set; }
        public int Odometro { get; set; }
        public string NotaFiscal { get; set; } = string.Empty;
        public string? Frentista { get; set; }
        public DateTime DataAbastecimento { get; set; }
        public string? Observacao { get; set; }
    }
}
