namespace Frota360.Application.DTOs.Auditoria.Request
{
    /// <summary>
    /// Filtro da tela de auditoria, vindo da query string. Todos os campos são opcionais
    /// menos a paginação, que tem defaults — a tela abre mostrando a primeira página.
    /// </summary>
    public class ConsultarAuditoriaRequest
    {
        public int Pagina { get; set; } = 1;

        /// <summary>Teto de 100 imposto pelo validator: sem ele um valor absurdo derruba a API.</summary>
        public int TamanhoPagina { get; set; } = 25;

        /// <summary>Uma constante de <c>EntidadesAuditadas</c>.</summary>
        public string? Entidade { get; set; }

        /// <summary>Uma constante de <c>AcoesAuditoria</c>.</summary>
        public string? Acao { get; set; }

        public int? UsuarioId { get; set; }

        public DateTime? De { get; set; }

        /// <summary>Inclusivo — o repositório estende até o fim do dia informado.</summary>
        public DateTime? Ate { get; set; }
    }
}
