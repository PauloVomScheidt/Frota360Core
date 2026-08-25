namespace Frota360.Application.DTOs.TipoManutencao.Response
{
    public class TipoManutencaoResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int? IntervaloKm { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
