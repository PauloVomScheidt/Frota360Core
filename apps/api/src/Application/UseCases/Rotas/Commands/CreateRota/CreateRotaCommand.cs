using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.DTOs.Rota.Response;

namespace Frota360.Application.UseCases.Rotas.Commands.CreateRota
{
    public sealed record CreateRotaCommand(CreateRotaRequest Data) : ICommand<RotaResponse>;
}
