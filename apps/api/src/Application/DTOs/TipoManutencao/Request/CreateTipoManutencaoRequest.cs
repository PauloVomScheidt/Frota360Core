namespace Frota360.Application.DTOs.TipoManutencao.Request
{
    public class CreateTipoManutencaoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public int? IntervaloKm { get; set; }
    }
}
