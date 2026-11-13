using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Request;
using Frota360.Application.DTOs.Despesa.Response;

namespace Frota360.Application.UseCases.Despesas.Commands.CreateDespesa
{
    public sealed record CreateDespesaCommand(CreateDespesaRequest Data) : ICommand<DespesaResponse>;
}
