using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Request;
using Frota360.Application.DTOs.Abastecimento.Response;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetResumoAbastecimentos
{
    /// <summary>
    /// Mesmo request da listagem de propósito: o rodapé precisa somar exatamente o recorte que a
    /// tabela mostra. <c>Pagina</c>/<c>TamanhoPagina</c> chegam junto e são ignorados.
    /// </summary>
    public sealed record GetResumoAbastecimentosQuery(ConsultarAbastecimentosRequest Filtro)
        : IQuery<ResumoAbastecimentosResponse>;
}
