namespace Frota360.Application.DTOs.TipoManutencao.Request
{
    public class UpdateTipoManutencaoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public int? IntervaloKm { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
