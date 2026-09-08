namespace Frota360.Application.DTOs.TipoDespesa.Request
{
    public class UpdateTipoDespesaRequest
    {
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
    }
}
