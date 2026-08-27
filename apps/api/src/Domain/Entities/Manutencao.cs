using Frota360.Domain.Enums;

namespace Frota360.Domain.Entities
{
    /// <summary>
    /// Manutenção de um veículo ao longo do seu ciclo: nasce planejada (quilometragem
    /// e/ou data previstas) e recebe os dados de execução quando concluída.
    /// </summary>
    public class Manutencao
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int VeiculoId { get; set; }
        public int TipoManutencaoId { get; set; }

        public int QuilometragemPrevista { get; set; }

        /// <summary>Prazo alternativo: manutenção vence no que vier primeiro, km ou data.</summary>
        public DateTime? DataPrevista { get; set; }

        public StatusManutencao Status { get; set; } = StatusManutencao.Pendente;
        public string? Observacao { get; set; }

        // Preenchidos na conclusão
        public int? QuilometragemRealizada { get; set; }
        public DateTime? DataRealizacao { get; set; }
        public decimal? Custo { get; set; }

        public DateTime DataInclusao { get; set; }

        // Navegação
        public Veiculo? Veiculo { get; set; }
        public TipoManutencao? Tipo { get; set; }
    }
}
