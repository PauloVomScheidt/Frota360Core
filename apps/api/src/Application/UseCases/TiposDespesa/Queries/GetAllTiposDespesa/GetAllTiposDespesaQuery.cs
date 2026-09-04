using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoDespesa.Response;

namespace Frota360.Application.UseCases.TiposDespesa.Queries.GetAllTiposDespesa
{
    public sealed record GetAllTiposDespesaQuery(bool ApenasAtivos = false) : IQuery<IEnumerable<TipoDespesaResponse>>;
}
