namespace Frota360.Application.DTOs.Custo.Request
{
    /// <summary>
    /// O mesmo recorte da lista, sem paginação: o resumo devolve os totais do período inteiro.
    /// </summary>
    public class ResumoCustosRequest
    {
        public int? VeiculoId { get; set; }

        public int? MotoristaId { get; set; }

        /// <summary>Um nome de <c>OrigemCusto</c>: <c>Abastecimento</c> ou <c>Manutencao</c>.</summary>
        public string? Origem { get; set; }

        public DateTime? De { get; set; }

        /// <summary>Inclusivo — o repositório estende até o fim do dia informado.</summary>
        public DateTime? Ate { get; set; }
    }
}
