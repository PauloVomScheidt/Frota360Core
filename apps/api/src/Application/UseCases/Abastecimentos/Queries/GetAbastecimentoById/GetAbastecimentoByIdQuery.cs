using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Response;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetAbastecimentoById
{
    public sealed record GetAbastecimentoByIdQuery(int Id) : IQuery<AbastecimentoResponse?>;
}
