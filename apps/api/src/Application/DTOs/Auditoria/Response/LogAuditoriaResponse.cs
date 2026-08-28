using Frota360.Application.Common;

namespace Frota360.Application.DTOs.Auditoria.Response
{
    /// <summary>
    /// Uma linha da trilha. Os dados de quem agiu vêm desnormalizados da própria linha —
    /// são o que era verdade no momento da ação, não o estado atual do usuário.
    /// </summary>
    public class LogAuditoriaResponse
    {
        public long Id { get; set; }

        public int UsuarioId { get; set; }
        public string UsuarioNome { get; set; } = string.Empty;
        public string UsuarioEmail { get; set; } = string.Empty;
        public string UsuarioRole { get; set; } = string.Empty;

        public string Entidade { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public int? EntidadeId { get; set; }

        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// Diff já desserializado — o front recebe a lista tipada, não a string JSON crua.
        /// Vazia em criação e exclusão.
        /// </summary>
        public IReadOnlyList<AlteracaoCampo> Alteracoes { get; set; } = [];

        public DateTime DataHora { get; set; }
        public string? IpOrigem { get; set; }
    }
}
