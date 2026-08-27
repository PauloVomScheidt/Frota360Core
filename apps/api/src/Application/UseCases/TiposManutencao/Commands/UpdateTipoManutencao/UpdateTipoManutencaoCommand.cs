using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoManutencao.Request;
using Frota360.Application.DTOs.TipoManutencao.Response;

namespace Frota360.Application.UseCases.TiposManutencao.Commands.UpdateTipoManutencao
{
    public sealed record UpdateTipoManutencaoCommand(int Id, UpdateTipoManutencaoRequest Data) : ICommand<TipoManutencaoResponse?>;
}
