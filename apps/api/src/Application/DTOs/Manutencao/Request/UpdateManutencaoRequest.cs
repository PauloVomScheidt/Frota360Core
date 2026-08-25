namespace Frota360.Application.DTOs.Manutencao.Request
{
    public class UpdateManutencaoRequest
    {
        public int VeiculoId { get; set; }
        public int TipoManutencaoId { get; set; }
        public int QuilometragemPrevista { get; set; }
        public DateTime? DataPrevista { get; set; }
        public string? Observacao { get; set; }
    }
}
