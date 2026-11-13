using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Custo.Request;
using Frota360.Application.DTOs.Custo.Response;

namespace Frota360.Application.UseCases.Custos.Queries.GetResumoCustos
{
    public sealed record GetResumoCustosQuery(ResumoCustosRequest Data) : IQuery<ResumoCustosResponse>;
}
