using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Domain.Enums;

namespace Frota360.Application.UseCases.Manutencoes.Queries.GetAllManutencoes
{
    /// <summary>Filtros opcionais: a tela abre por veículo e alterna entre pendentes e histórico.</summary>
    public sealed record GetAllManutencoesQuery(int? VeiculoId = null, StatusManutencao? Status = null)
        : IQuery<IEnumerable<ManutencaoResponse>>;
}
