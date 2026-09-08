namespace Frota360.Application.DTOs.Custo.Response
{
    /// <summary>
    /// Total de um mês do período, com as origens separadas — alimenta o gráfico da tela.
    /// Só aparecem os meses que tiveram lançamento.
    /// </summary>
    public class CustoPorMesResponse
    {
        public int Ano { get; set; }

        /// <summary>1 a 12.</summary>
        public int Mes { get; set; }

        public decimal TotalAbastecimento { get; set; }

        public decimal TotalManutencao { get; set; }

        public decimal TotalDespesa { get; set; }

        public decimal Total { get; set; }
    }
}
