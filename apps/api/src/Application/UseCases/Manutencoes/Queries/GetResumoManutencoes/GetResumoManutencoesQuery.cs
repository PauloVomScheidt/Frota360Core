using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Response;

namespace Frota360.Application.UseCases.Manutencoes.Queries.GetResumoManutencoes
{
    public sealed record GetResumoManutencoesQuery : IQuery<ResumoManutencoesResponse>;
}
