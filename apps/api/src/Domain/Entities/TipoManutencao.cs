namespace Frota360.Domain.Entities
{
    /// <summary>
    /// Catálogo de tipos de manutenção da empresa ("Troca de óleo", "Troca de pneus").
    /// Cada empresa mantém o seu, semeado com um conjunto padrão no provisionamento.
    /// </summary>
    public class TipoManutencao
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = string.Empty;

        /// <summary>Periodicidade em km ("óleo a cada 10.000"). Opcional; base da recorrência automática.</summary>
        public int? IntervaloKm { get; set; }

        public bool Ativo { get; set; } = true;
        public DateTime DataInclusao { get; set; }
    }
}
