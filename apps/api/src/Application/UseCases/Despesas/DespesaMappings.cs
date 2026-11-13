using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.Despesas
{
    /// <summary>Mapeamento centralizado de <see cref="Despesa"/> para <see cref="DespesaResponse"/>.</summary>
    public static class DespesaMappings
    {
        public static DespesaResponse ToResponse(this Despesa d) => new()
        {
            Id = d.Id,
            VeiculoId = d.VeiculoId,
            VeiculoNome = d.Veiculo?.NomeVeiculo ?? string.Empty,
            VeiculoPlaca = d.Veiculo?.Placa ?? string.Empty,
            TipoDespesaId = d.TipoDespesaId,
            TipoDespesaNome = d.Tipo?.Nome ?? string.Empty,
            MotoristaId = d.MotoristaId,
            MotoristaNome = d.Motorista?.Nome,
            Valor = d.Valor,
            DataDespesa = d.DataDespesa,
            Observacao = d.Observacao,
            DataInclusao = d.DataInclusao
        };
    }
}
