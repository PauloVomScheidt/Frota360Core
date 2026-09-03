namespace Frota360.Application.DTOs.Custo.Response
{
    /// <summary>
    /// Os números da tela de custos, somados no banco. É a primeira agregação servida pela
    /// API — as telas de relatório futuras saem daqui, não de <c>reduce</c> no cliente.
    /// </summary>
    public class ResumoCustosResponse
    {
        public decimal Total { get; set; }

        public decimal TotalAbastecimento { get; set; }

        public decimal TotalManutencao { get; set; }

        public int QuantidadeLancamentos { get; set; }

        /// <summary>Km das rotas encerradas no período, somado da frota inteira.</summary>
        public int KmTotal { get; set; }

        /// <summary>
        /// Nulo quando não houve rota encerrada no período. Rota ainda aberta não tem
        /// quilometragem apurada, então o mês corrente subestima o km e superestima o R$/km.
        /// </summary>
        public decimal? CustoPorKm { get; set; }

        /// <summary>
        /// Manutenções concluídas no período sem custo informado. Elas não entram em soma
        /// nenhuma — a tela mostra a contagem para o total não mentir por omissão.
        /// </summary>
        public int ManutencoesSemCustoInformado { get; set; }

        /// <summary>Do maior total para o menor.</summary>
        public List<CustoPorVeiculoResponse> PorVeiculo { get; set; } = [];

        /// <summary>Em ordem cronológica.</summary>
        public List<CustoPorMesResponse> PorMes { get; set; } = [];
    }
}
