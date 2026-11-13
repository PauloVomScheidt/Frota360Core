namespace Frota360.Application.DTOs.Custo.Response
{
    /// <summary>
    /// Uma linha da lista de custos, já normalizada entre as origens.
    /// </summary>
    public class LancamentoCustoResponse
    {
        /// <summary>Nome de <c>OrigemCusto</c>, como o status da manutenção também viaja em texto.</summary>
        public string Origem { get; set; } = string.Empty;

        /// <summary>Id na tabela de origem — é por ele que a tela volta ao registro.</summary>
        public int OrigemId { get; set; }

        public DateTime Data { get; set; }

        public int VeiculoId { get; set; }

        public string VeiculoNome { get; set; } = string.Empty;

        public string VeiculoPlaca { get; set; } = string.Empty;

        /// <summary>Nulo em manutenção, que não é atribuída a motorista.</summary>
        public int? MotoristaId { get; set; }

        public string? MotoristaNome { get; set; }

        /// <summary>"Combustível" no abastecimento; o nome do tipo na manutenção.</summary>
        public string Categoria { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public string? Observacao { get; set; }
    }
}
