namespace Frota360.Application.DTOs.Manutencao.Response
{
    public class ResumoManutencoesResponse
    {
        public int Atrasadas { get; set; }

        /// <summary>Pendentes ainda não vencidas a até <c>RegrasDeManutencao.FaixaAvisoKm</c> do previsto.</summary>
        public int Vencendo { get; set; }

        /// <summary>Atrasadas primeiro, depois vencendo; dentro de cada grupo, o menor km restante antes.</summary>
        public IEnumerable<ManutencaoResponse> Urgentes { get; set; } = [];
    }
}
