using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Posto.Request;
using Frota360.Application.DTOs.Posto.Response;

namespace Frota360.Application.UseCases.Postos.Commands.CreatePosto
{
    public sealed record CreatePostoCommand(CreatePostoRequest Data) : ICommand<PostoResponse>;
}
