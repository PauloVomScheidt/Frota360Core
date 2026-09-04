namespace Frota360.Application.DTOs.TipoDespesa.Response
{
    public class TipoDespesaResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
