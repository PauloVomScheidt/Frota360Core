using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Request;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Domain.Common;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetAllAbastecimentos
{
    /// <summary>
    /// Uma página da listagem. <c>MotoristaId</c> do filtro serve à gestão (gasto por motorista);
    /// para a role Motorista o handler o sobrescreve com o usuário do token — o recorte por pessoa
    /// nunca é escolha do cliente.
    /// </summary>
    public sealed record GetAllAbastecimentosQuery(ConsultarAbastecimentosRequest Filtro)
        : IQuery<ResultadoPaginado<AbastecimentoResponse>>;
}
