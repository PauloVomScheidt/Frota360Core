using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Response;

namespace Frota360.Application.UseCases.Despesas.Queries.GetAllDespesas
{
    public sealed record GetAllDespesasQuery(int? VeiculoId = null, int? MotoristaId = null,
                                             int? TipoDespesaId = null, DateTime? De = null, DateTime? Ate = null)
        : IQuery<IEnumerable<DespesaResponse>>;
}
