using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Response;

namespace Frota360.Application.UseCases.Despesas.Queries.GetDespesaById
{
    public sealed record GetDespesaByIdQuery(int Id) : IQuery<DespesaResponse?>;
}
