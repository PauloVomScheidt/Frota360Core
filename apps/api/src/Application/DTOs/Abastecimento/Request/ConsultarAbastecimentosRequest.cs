using Frota360.Application.Common;

namespace Frota360.Application.DTOs.Abastecimento.Request
{
    /// <summary>
    /// Filtro da lista de abastecimentos, vindo da query string. Todos os campos são opcionais
    /// menos a paginação, que tem defaults — a tela abre na primeira página.
    ///
    /// O mesmo request serve <c>GET /abastecimento</c> e <c>GET /abastecimento/resumo</c>: os dois
    /// precisam enxergar o mesmo recorte, e repetir a lista de filtros em dois DTOs seria convite
    /// para eles divergirem.
    /// </summary>
    public class ConsultarAbastecimentosRequest : IRequestPaginado
    {
        public int Pagina { get; set; } = 1;

        /// <summary>Teto de 100 imposto pelo validator: sem ele um valor absurdo derruba a API.</summary>
        public int TamanhoPagina { get; set; } = 15;

        public int? VeiculoId { get; set; }

        /// <summary>
        /// Serve à gestão (gasto por motorista). ⚠️ Para a role Motorista o handler <b>sobrescreve</b>
        /// este campo com o usuário do token — o recorte por pessoa nunca é escolha do cliente.
        /// </summary>
        public int? MotoristaId { get; set; }

        public DateTime? De { get; set; }

        /// <summary>Inclusivo — o repositório estende até o fim do dia informado.</summary>
        public DateTime? Ate { get; set; }
    }
}
