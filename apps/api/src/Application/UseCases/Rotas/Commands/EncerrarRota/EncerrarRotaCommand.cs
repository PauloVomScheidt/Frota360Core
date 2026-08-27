using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.DTOs.Rota.Response;

namespace Frota360.Application.UseCases.Rotas.Commands.EncerrarRota
{
    public sealed record EncerrarRotaCommand(int Id, EncerrarRotaRequest Data) : ICommand<RotaResponse?>;
}
