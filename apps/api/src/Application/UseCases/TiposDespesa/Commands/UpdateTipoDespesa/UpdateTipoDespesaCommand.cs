using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoDespesa.Request;
using Frota360.Application.DTOs.TipoDespesa.Response;

namespace Frota360.Application.UseCases.TiposDespesa.Commands.UpdateTipoDespesa
{
    public sealed record UpdateTipoDespesaCommand(int Id, UpdateTipoDespesaRequest Data) : ICommand<TipoDespesaResponse?>;
}
