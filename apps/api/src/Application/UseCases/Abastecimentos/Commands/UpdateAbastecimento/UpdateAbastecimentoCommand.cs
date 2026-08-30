using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Request;
using Frota360.Application.DTOs.Abastecimento.Response;

namespace Frota360.Application.UseCases.Abastecimentos.Commands.UpdateAbastecimento
{
    public sealed record UpdateAbastecimentoCommand(int Id, UpdateAbastecimentoRequest Data)
        : ICommand<AbastecimentoResponse?>;
}
