using Frota360.Application.Common;

namespace Frota360.Application.DTOs.Rota.Request
{
    /// <summary>
    /// Filtro da lista de rotas, vindo da query string. Serve tanto <c>GET /rota</c> (gestão)
    /// quanto <c>GET /rota/minhas</c> (motorista) — no segundo, o recorte por pessoa vem do token
    /// e não daqui.
    /// </summary>
    public class ConsultarRotasRequest : IRequestPaginado
    {
        public int Pagina { get; set; } = 1;

        /// <summary>Teto de 100 imposto pelo validator: sem ele um valor absurdo derruba a API.</summary>
        public int TamanhoPagina { get; set; } = 15;

        /// <summary>
        /// <c>true</c> traz só as rotas em andamento, <c>false</c> só o histórico, nulo traz tudo.
        ///
        /// É o que a tela do motorista usa para achar a rota ativa sem baixar a lista inteira, e o
        /// que o dashboard usa para contar rotas abertas — pedindo `tamanhoPagina=1` e lendo o
        /// <c>total</c> da resposta, que não precisa de endpoint de contagem.
        /// </summary>
        public bool? Ativo { get; set; }
    }
}
