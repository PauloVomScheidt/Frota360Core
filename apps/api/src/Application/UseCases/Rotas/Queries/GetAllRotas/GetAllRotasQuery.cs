using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Domain.Common;

namespace Frota360.Application.UseCases.Rotas.Queries.GetAllRotas
{
    public sealed record GetAllRotasQuery(ConsultarRotasRequest Filtro)
        : IQuery<ResultadoPaginado<RotaResponse>>;
}
