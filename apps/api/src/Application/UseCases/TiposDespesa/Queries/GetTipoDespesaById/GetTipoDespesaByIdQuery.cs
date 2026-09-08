using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoDespesa.Response;

namespace Frota360.Application.UseCases.TiposDespesa.Queries.GetTipoDespesaById
{
    public sealed record GetTipoDespesaByIdQuery(int Id) : IQuery<TipoDespesaResponse?>;
}
