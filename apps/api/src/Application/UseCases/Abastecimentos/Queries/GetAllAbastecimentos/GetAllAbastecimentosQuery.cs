using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Response;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetAllAbastecimentos
{
    /// <summary>
    /// Filtros opcionais da tela. <see cref="MotoristaId"/> serve à gestão (gasto por
    /// motorista); para a role Motorista o handler o sobrescreve com o usuário do token —
    /// o recorte por pessoa nunca é escolha do cliente.
    /// </summary>
    public sealed record GetAllAbastecimentosQuery(int? VeiculoId = null, int? MotoristaId = null,
                                                   DateTime? De = null, DateTime? Ate = null)
        : IQuery<IEnumerable<AbastecimentoResponse>>;
}
