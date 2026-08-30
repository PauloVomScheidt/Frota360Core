namespace Frota360.Application.DTOs.Abastecimento.Request
{
    /// <summary>
    /// Correção de um lançamento — errar o valor digitado no posto é comum. Veículo e
    /// motorista não entram: trocar qualquer um dos dois reescreveria a atribuição do gasto,
    /// e o vínculo com a rota foi resolvido no momento do lançamento. Nesse caso, exclua e
    /// lance de novo.
    /// </summary>
    public class UpdateAbastecimentoRequest
    {
        public decimal Valor { get; set; }
        public DateTime DataAbastecimento { get; set; }
        public string? Observacao { get; set; }
    }
}
