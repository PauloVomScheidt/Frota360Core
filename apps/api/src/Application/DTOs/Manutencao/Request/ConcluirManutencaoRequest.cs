namespace Frota360.Application.DTOs.Manutencao.Request
{
    public class ConcluirManutencaoRequest
    {
        public int QuilometragemRealizada { get; set; }
        public DateTime DataRealizacao { get; set; }
        public decimal? Custo { get; set; }
        public string? Observacao { get; set; }
    }
}
