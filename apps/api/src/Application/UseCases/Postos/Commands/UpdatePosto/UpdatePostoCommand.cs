using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Posto.Request;
using Frota360.Application.DTOs.Posto.Response;

namespace Frota360.Application.UseCases.Postos.Commands.UpdatePosto
{
    public sealed record UpdatePostoCommand(int Id, UpdatePostoRequest Data) : ICommand<PostoResponse?>;
}
