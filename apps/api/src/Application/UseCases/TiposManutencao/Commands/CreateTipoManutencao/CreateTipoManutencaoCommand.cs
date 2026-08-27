using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoManutencao.Request;
using Frota360.Application.DTOs.TipoManutencao.Response;

namespace Frota360.Application.UseCases.TiposManutencao.Commands.CreateTipoManutencao
{
    public sealed record CreateTipoManutencaoCommand(CreateTipoManutencaoRequest Data) : ICommand<TipoManutencaoResponse>;
}
