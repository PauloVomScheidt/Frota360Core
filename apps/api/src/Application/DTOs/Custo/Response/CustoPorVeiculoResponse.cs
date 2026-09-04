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

        /// <summary>Litros abastecidos no período, sem os do primeiro abastecimento — ver ConsumoPorVeiculo.</summary>
        public decimal Litros { get; set; }

        /// <summary>
        /// Km medido pelo <b>odômetro dos abastecimentos</b> — diferente de <c>Km</c>, que vem
        /// das rotas encerradas. São duas medidas distintas, e a tela diz qual é qual.
        /// </summary>
        public int KmOdometro { get; set; }

        /// <summary>
        /// Consumo médio em km/l. Nulo quando o veículo teve menos de dois abastecimentos no
        /// período (sem intervalo não há métrica) ou quando o odômetro não avançou.
        /// </summary>
        public decimal? ConsumoMedio { get; set; }
    }
}
