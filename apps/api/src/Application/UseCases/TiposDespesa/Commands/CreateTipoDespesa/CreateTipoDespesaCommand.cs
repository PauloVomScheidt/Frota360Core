using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoDespesa.Request;
using Frota360.Application.DTOs.TipoDespesa.Response;

namespace Frota360.Application.UseCases.TiposDespesa.Commands.CreateTipoDespesa
{
    public sealed record CreateTipoDespesaCommand(CreateTipoDespesaRequest Data) : ICommand<TipoDespesaResponse>;
}
