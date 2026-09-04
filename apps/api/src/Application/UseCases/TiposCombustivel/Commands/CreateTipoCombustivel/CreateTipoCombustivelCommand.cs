using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoCombustivel.Request;
using Frota360.Application.DTOs.TipoCombustivel.Response;

namespace Frota360.Application.UseCases.TiposCombustivel.Commands.CreateTipoCombustivel
{
    public sealed record CreateTipoCombustivelCommand(CreateTipoCombustivelRequest Data) : ICommand<TipoCombustivelResponse>;
}
