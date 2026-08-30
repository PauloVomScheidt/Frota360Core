using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Request;
using Frota360.Application.DTOs.Abastecimento.Response;

namespace Frota360.Application.UseCases.Abastecimentos.Commands.CreateAbastecimento
{
    public sealed record CreateAbastecimentoCommand(CreateAbastecimentoRequest Data) : ICommand<AbastecimentoResponse>;
}
