using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Domain.Entities;
using Frota360.Domain.Enums;

namespace Frota360.Application.UseCases.Manutencoes
{
    /// <summary>
    /// Mapeamento centralizado de <see cref="Manutencao"/> para <see cref="ManutencaoResponse"/>.
    /// É aqui que "atrasada" e "km restantes" nascem: comparando o previsto com a quilometragem
    /// atual do veículo no instante da leitura, em vez de manter um status envelhecido no banco.
    /// </summary>
    public static class ManutencaoMappings
    {
        public static ManutencaoResponse ToResponse(this Manutencao m)
        {
            var kmAtual = m.Veiculo?.Quilometragem ?? 0;
            var pendente = m.Status == StatusManutencao.Pendente;

            return new ManutencaoResponse
            {
                Id = m.Id,
                VeiculoId = m.VeiculoId,
                VeiculoNome = m.Veiculo?.NomeVeiculo ?? string.Empty,
                VeiculoPlaca = m.Veiculo?.Placa ?? string.Empty,
                TipoManutencaoId = m.TipoManutencaoId,
                TipoManutencaoNome = m.Tipo?.Nome ?? string.Empty,
                QuilometragemPrevista = m.QuilometragemPrevista,
                DataPrevista = m.DataPrevista,
                Status = m.Status.ToString(),
                Observacao = m.Observacao,
                QuilometragemAtualVeiculo = kmAtual,
                KmRestantes = pendente ? m.QuilometragemPrevista - kmAtual : null,
                Atrasada = pendente && VenceuPor(m, kmAtual),
                QuilometragemRealizada = m.QuilometragemRealizada,
                DataRealizacao = m.DataRealizacao,
                Custo = m.Custo,
                DataInclusao = m.DataInclusao
            };
        }

        /// <summary>Vence no que vier primeiro: quilometragem atingida ou data prevista alcançada.</summary>
        private static bool VenceuPor(Manutencao m, int kmAtual)
            => kmAtual >= m.QuilometragemPrevista
               || (m.DataPrevista.HasValue && m.DataPrevista.Value.Date <= DateTime.Now.Date);
    }
}
