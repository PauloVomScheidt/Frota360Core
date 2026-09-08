using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Request;
using Frota360.Application.DTOs.Despesa.Response;

namespace Frota360.Application.UseCases.Despesas.Commands.UpdateDespesa
{
    public sealed record UpdateDespesaCommand(int Id, UpdateDespesaRequest Data) : ICommand<DespesaResponse?>;
}
