namespace Frota360.Application.DTOs.Custo.Response
{
    /// <summary>
    /// Total de um veículo no período, com as origens já separadas em colunas.
    /// </summary>
    public class CustoPorVeiculoResponse
    {
        public int VeiculoId { get; set; }

        public string VeiculoNome { get; set; } = string.Empty;

        public string VeiculoPlaca { get; set; } = string.Empty;

        public decimal TotalAbastecimento { get; set; }

        public decimal TotalManutencao { get; set; }

        public decimal TotalDespesa { get; set; }

        public decimal Total { get; set; }

        /// <summary>Km das rotas encerradas no período. Zero quando nenhuma foi encerrada.</summary>
        public int Km { get; set; }

        /// <summary>Nulo quando <c>Km</c> é zero — não há denominador.</summary>
        public decimal? CustoPorKm { get; set; }
    }
}
