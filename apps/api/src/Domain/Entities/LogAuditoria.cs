namespace Frota360.Domain.Entities
{
    /// <summary>
    /// Trilha de auditoria da empresa: uma linha por operação de escrita relevante.
    /// É <b>append-only</b> — não há endpoint de update nem de delete, e o repositório
    /// só expõe inserção e consulta.
    /// </summary>
    public class LogAuditoria
    {
        /// <summary>long porque a tabela cresce sem política de purga.</summary>
        public long Id { get; set; }

        public int EmpresaId { get; set; }

        public int UsuarioId { get; set; }

        /// <summary>
        /// Nome, e-mail e papel de quem agiu ficam <b>desnormalizados</b>: o log é histórico
        /// e não pode mudar de sentido quando o usuário é renomeado ou rebaixado depois.
        /// Mesma técnica de <c>RotaResponse.NomeMotorista</c>.
        /// </summary>
        public string UsuarioNome { get; set; } = string.Empty;
        public string UsuarioEmail { get; set; } = string.Empty;
        public string UsuarioRole { get; set; } = string.Empty;

        /// <summary>O que foi tocado — ver <c>EntidadesAuditadas</c>.</summary>
        public string Entidade { get; set; } = string.Empty;

        /// <summary>O que foi feito — ver <c>AcoesAuditoria</c>.</summary>
        public string Acao { get; set; } = string.Empty;

        /// <summary>Id do registro afetado. Nulo só onde a ação não tem alvo identificável.</summary>
        public int? EntidadeId { get; set; }

        /// <summary>
        /// Frase pronta em português, exibida na listagem — o log não é traduzido em tempo
        /// de leitura. Ex.: "Encerrou a rota #12 (São Paulo → Campinas) com 340 km rodados".
        /// </summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// Diff campo a campo em JSON: <c>[{"campo":"Placa","de":"ABC1D23","para":"XYZ9K87"}]</c>.
        /// Nulo em criação e exclusão, onde não existe "antes e depois".
        /// <b>Nunca</b> recebe hash de senha, refresh token, token de reset ou de convite.
        /// </summary>
        public string? Alteracoes { get; set; }

        /// <summary>Hora local de Brasília, como todo o resto do sistema.</summary>
        public DateTime DataHora { get; set; }

        /// <summary>Comporta IPv6 (45 caracteres).</summary>
        public string? IpOrigem { get; set; }
    }
}
