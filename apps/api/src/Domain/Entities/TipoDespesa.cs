namespace Frota360.Domain.Entities
{
    /// <summary>
    /// Catálogo de tipos de despesa da empresa ("Pedágio", "IPVA", "Multa de trânsito").
    /// Cada empresa mantém o seu, semeado com um conjunto padrão no provisionamento —
    /// mesmo desenho de <see cref="TipoManutencao"/>.
    /// </summary>
    public class TipoDespesa
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Tipo em uso não é excluído, é inativado: apagá-lo levaria junto o histórico
        /// financeiro. Inativo some do seletor de lançamento e continua nomeando o passado.
        /// </summary>
        public bool Ativo { get; set; } = true;

        public DateTime DataInclusao { get; set; }
    }
}
