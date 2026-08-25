using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoManutencao.Response;

namespace Frota360.Application.UseCases.TiposManutencao.Queries.GetAllTiposManutencao
{
    /// <summary>O dropdown da tela de manutenção pede apenasAtivos; a tela de cadastro lista tudo.</summary>
    public sealed record GetAllTiposManutencaoQuery(bool ApenasAtivos = false) : IQuery<IEnumerable<TipoManutencaoResponse>>;
}
