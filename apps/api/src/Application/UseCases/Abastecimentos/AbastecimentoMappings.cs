using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.Abastecimentos
{
    /// <summary>Mapeamento centralizado de <see cref="Abastecimento"/> para <see cref="AbastecimentoResponse"/>.</summary>
    public static class AbastecimentoMappings
    {
        public static AbastecimentoResponse ToResponse(this Abastecimento a) => new()
        {
            Id = a.Id,
            VeiculoId = a.VeiculoId,
            VeiculoNome = a.Veiculo?.NomeVeiculo ?? string.Empty,
            VeiculoPlaca = a.Veiculo?.Placa ?? string.Empty,
            RotaId = a.RotaId,
            RotaDescricao = a.Rota is null ? null : $"{a.Rota.Origem} → {a.Rota.Destino}",
            MotoristaId = a.MotoristaId,
            MotoristaNome = a.Motorista?.Nome ?? string.Empty,
            UsuarioId = a.UsuarioId,
            UsuarioNome = a.Usuario?.Nome ?? string.Empty,
            Valor = a.Valor,
            DataAbastecimento = a.DataAbastecimento,
            Observacao = a.Observacao,
            DataInclusao = a.DataInclusao
        };
    }
}
