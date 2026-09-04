namespace Frota360.Domain.Entities
{
    /// <summary>
    /// Catálogo de combustíveis da empresa ("Diesel S10", "Gasolina comum", "Etanol").
    /// Cada empresa mantém o seu, semeado com um conjunto padrão no provisionamento —
    /// mesmo desenho de <see cref="TipoDespesa"/>.
    /// </summary>
    public class TipoCombustivel
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Combustível em uso não é excluído, é inativado: apagá-lo levaria junto o
        /// histórico de abastecimento. Inativo some do seletor de lançamento e continua
        /// nomeando o passado.
        /// </summary>
        public bool Ativo { get; set; } = true;

        public DateTime DataInclusao { get; set; }
    }
}
