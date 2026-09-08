using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoCombustivel.Request;
using Frota360.Application.DTOs.TipoCombustivel.Response;

namespace Frota360.Application.UseCases.TiposCombustivel.Commands.UpdateTipoCombustivel
{
    public sealed record UpdateTipoCombustivelCommand(int Id, UpdateTipoCombustivelRequest Data) : ICommand<TipoCombustivelResponse?>;
}
