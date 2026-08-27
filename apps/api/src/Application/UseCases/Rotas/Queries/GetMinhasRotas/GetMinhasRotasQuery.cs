using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;

namespace Frota360.Application.UseCases.Rotas.Queries.GetMinhasRotas
{
    /// <summary>
    /// Sem parâmetros de propósito: o motorista vem da claim, nunca do cliente.
    /// </summary>
    public sealed record GetMinhasRotasQuery : IQuery<IEnumerable<RotaResponse>>;
}
