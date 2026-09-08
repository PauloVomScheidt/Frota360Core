using Frota360.Application.Common;

namespace Frota360.Application.DTOs.Despesa.Request
{
    /// <summary>
    /// Filtro da lista de despesas, vindo da query string. Todos os campos são opcionais menos a
    /// paginação, que tem defaults — a tela abre na primeira página.
    ///
    /// O mesmo request serve <c>GET /despesa</c> e <c>GET /despesa/resumo</c>: os dois precisam
    /// enxergar o mesmo recorte, e repetir a lista de filtros em dois DTOs seria convite para eles
    /// divergirem.
    /// </summary>
    public class ConsultarDespesasRequest : IRequestPaginado
    {
        public int Pagina { get; set; } = 1;

        /// <summary>Teto de 100 imposto pelo validator: sem ele um valor absurdo derruba a API.</summary>
        public int TamanhoPagina { get; set; } = 15;

        public int? VeiculoId { get; set; }

        /// <summary>
        /// Filtro de relatório. Diferente do abastecimento, aqui não há segundo eixo: `/despesas`
        /// é tela de gestão e o motorista sequer a enxerga.
        /// </summary>
        public int? MotoristaId { get; set; }

        public int? TipoDespesaId { get; set; }

        public DateTime? De { get; set; }

        /// <summary>Inclusivo — o repositório estende até o fim do dia informado.</summary>
        public DateTime? Ate { get; set; }
    }
}
