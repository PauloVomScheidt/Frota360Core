using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Request;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Domain.Common;

namespace Frota360.Application.UseCases.Manutencoes.Queries.GetAllManutencoes
{
    public sealed record GetAllManutencoesQuery(ConsultarManutencoesRequest Filtro)
        : IQuery<ResultadoPaginado<ManutencaoResponse>>;
}
