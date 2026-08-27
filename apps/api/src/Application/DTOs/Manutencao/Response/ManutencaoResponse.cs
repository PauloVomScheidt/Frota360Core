namespace Frota360.Application.DTOs.Manutencao.Response
{
    public class ManutencaoResponse
    {
        public int Id { get; set; }

        public int VeiculoId { get; set; }
        public string VeiculoNome { get; set; } = string.Empty;
        public string VeiculoPlaca { get; set; } = string.Empty;

        public int TipoManutencaoId { get; set; }
        public string TipoManutencaoNome { get; set; } = string.Empty;

        public int QuilometragemPrevista { get; set; }
        public DateTime? DataPrevista { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Observacao { get; set; }

        /// <summary>Quilometragem do veículo no momento da consulta, para a tela comparar sem outra chamada.</summary>
        public int QuilometragemAtualVeiculo { get; set; }

        /// <summary>Quanto falta para vencer. Negativo quando já passou; nulo se a manutenção não está pendente.</summary>
        public int? KmRestantes { get; set; }

        /// <summary>Derivado na leitura: pendente e já passou do km previsto (ou da data prevista).</summary>
        public bool Atrasada { get; set; }

        public int? QuilometragemRealizada { get; set; }
        public DateTime? DataRealizacao { get; set; }
        public decimal? Custo { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
