namespace Frota360.Domain.Common
{
    /// <summary>
    /// Página de uma listagem. Vai <b>dentro</b> de <c>ApiResponse&lt;T&gt;.Dados</c>, sem mexer
    /// no envelope: <c>ApiResponse&lt;ResultadoPaginado&lt;XResponse&gt;&gt;</c>.
    ///
    /// Nasceu com a trilha de auditoria — a primeira listagem do sistema que não cabe de uma vez.
    /// As demais continuam devolvendo a coleção inteira; use isto quando o volume crescer.
    /// </summary>
    public class ResultadoPaginado<T>
    {
        public IEnumerable<T> Itens { get; set; } = [];

        /// <summary>Página atual, começando em 1.</summary>
        public int Pagina { get; set; }

        public int TamanhoPagina { get; set; }

        /// <summary>Total de registros que satisfazem o filtro, ignorando a paginação.</summary>
        public int Total { get; set; }

        public int TotalPaginas => TamanhoPagina <= 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanhoPagina);
    }
}
