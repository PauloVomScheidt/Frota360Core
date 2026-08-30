namespace Frota360.Application.DTOs.Abastecimento.Response
{
    public class AbastecimentoResponse
    {
        public int Id { get; set; }

        public int VeiculoId { get; set; }
        /// <summary>Desnormalizados, como em ManutencaoResponse: a listagem não faz join no cliente.</summary>
        public string VeiculoNome { get; set; } = string.Empty;
        public string VeiculoPlaca { get; set; } = string.Empty;

        public int? RotaId { get; set; }
        /// <summary>"Origem → Destino" da rota vinculada, quando há uma.</summary>
        public string? RotaDescricao { get; set; }

        /// <summary>De quem é o gasto.</summary>
        public int MotoristaId { get; set; }
        public string MotoristaNome { get; set; } = string.Empty;

        /// <summary>Quem digitou — diferente do motorista quando a gestão lança por ele.</summary>
        public int UsuarioId { get; set; }
        public string UsuarioNome { get; set; } = string.Empty;

        public decimal Valor { get; set; }
        public DateTime DataAbastecimento { get; set; }
        public string? Observacao { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
