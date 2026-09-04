using Frota360.Application.Common;
using Frota360.Domain.Enums;

namespace Frota360.Application.DTOs.Manutencao.Request
{
    /// <summary>
    /// Filtro da lista de manutenções, vindo da query string. A tela abre por veículo, alterna
    /// entre pendentes e histórico e recorta por período — o período incide sobre a data relevante
    /// do status (prazo quando pendente, execução quando realizada).
    /// </summary>
    public class ConsultarManutencoesRequest : IRequestPaginado
    {
        public int Pagina { get; set; } = 1;

        /// <summary>Teto de 100 imposto pelo validator: sem ele um valor absurdo derruba a API.</summary>
        public int TamanhoPagina { get; set; } = 15;

        public int? VeiculoId { get; set; }

        public StatusManutencao? Status { get; set; }

        public DateTime? De { get; set; }

        /// <summary>Inclusivo — o repositório estende até o fim do dia informado.</summary>
        public DateTime? Ate { get; set; }
    }
}
