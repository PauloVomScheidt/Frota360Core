namespace Frota360.Application.DTOs.TipoCombustivel.Response
{
    public class TipoCombustivelResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
