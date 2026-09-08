namespace Frota360.Domain.Entities
{
    /// <summary>
    /// A rede credenciada da empresa. O abastecimento aponta para um posto daqui, e é o
    /// que permite responder "quanto gastamos no posto X" — diferente do catálogo de
    /// combustível, aqui não há conjunto padrão: cada empresa credencia os seus.
    /// </summary>
    public class Posto
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = string.Empty;

        /// <summary>Opcional: nem todo posto credenciado é registrado com nota da empresa.</summary>
        public string? Cnpj { get; set; }

        public string? Cidade { get; set; }

        /// <summary>
        /// Posto descredenciado é inativado, não excluído: apagá-lo levaria junto o
        /// histórico de abastecimento.
        /// </summary>
        public bool Ativo { get; set; } = true;

        public DateTime DataInclusao { get; set; }
    }
}
