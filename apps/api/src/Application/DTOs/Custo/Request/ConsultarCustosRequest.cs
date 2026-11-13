namespace Frota360.Application.DTOs.Custo.Request
{
    /// <summary>
    /// Filtro da lista de custos, vindo da query string. Todos os campos são opcionais menos
    /// a paginação, que tem defaults — a tela abre na primeira página.
    /// </summary>
    public class ConsultarCustosRequest
    {
        public int Pagina { get; set; } = 1;

        /// <summary>Teto de 100 imposto pelo validator: sem ele um valor absurdo derruba a API.</summary>
        public int TamanhoPagina { get; set; } = 25;

        public int? VeiculoId { get; set; }

        /// <summary>
        /// Preenchido, o resultado sai só com abastecimentos: manutenção não é atribuída a
        /// motorista no modelo. A tela avisa o usuário disso.
        /// </summary>
        public int? MotoristaId { get; set; }

        /// <summary>Um nome de <c>OrigemCusto</c>: <c>Abastecimento</c> ou <c>Manutencao</c>.</summary>
        public string? Origem { get; set; }

        public DateTime? De { get; set; }

        /// <summary>Inclusivo — o repositório estende até o fim do dia informado.</summary>
        public DateTime? Ate { get; set; }
    }
}
