using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Domain.Enums;

namespace Frota360.Application.UseCases.Manutencoes.Queries.GetAllManutencoes
{
    /// <summary>
    /// Filtros opcionais: a tela abre por veículo, alterna entre pendentes e histórico e
    /// recorta por período. O período incide sobre a data relevante do status — prazo
    /// quando pendente, execução quando realizada.
    /// </summary>
    public sealed record GetAllManutencoesQuery(int? VeiculoId = null, StatusManutencao? Status = null,
                                                DateTime? De = null, DateTime? Ate = null)
        : IQuery<IEnumerable<ManutencaoResponse>>;
}
