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
            TipoCombustivelId = a.TipoCombustivelId,
            TipoCombustivelNome = a.TipoCombustivel?.Nome ?? string.Empty,
            PostoId = a.PostoId,
            PostoNome = a.Posto?.Nome ?? string.Empty,
            Litros = a.Litros,
            ValorLitro = a.ValorLitro,
            Valor = a.Valor,
            Odometro = a.Odometro,
            NotaFiscal = a.NotaFiscal,
            Frentista = a.Frentista,
            DataAbastecimento = a.DataAbastecimento,
            Observacao = a.Observacao,
            DataInclusao = a.DataInclusao
        };

        /// <summary>
        /// O total nunca vem do cliente: a tela o exibe como readonly e o servidor o
        /// recalcula a cada escrita. É o que impede o resumo de custos de divergir do
        /// apontamento — ele é a única fonte do gasto com combustível.
        /// </summary>
        public static decimal CalcularValor(decimal litros, decimal valorLitro)
            => Math.Round(litros * valorLitro, 2, MidpointRounding.AwayFromZero);
    }
}
