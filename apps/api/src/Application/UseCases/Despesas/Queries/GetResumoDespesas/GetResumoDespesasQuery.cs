using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Request;
using Frota360.Application.DTOs.Despesa.Response;

namespace Frota360.Application.UseCases.Despesas.Queries.GetResumoDespesas
{
    /// <summary>
    /// Mesmo request da listagem de propósito: o rodapé precisa somar exatamente o recorte que a
    /// tabela mostra. <c>Pagina</c>/<c>TamanhoPagina</c> chegam junto e são ignorados.
    /// </summary>
    public sealed record GetResumoDespesasQuery(ConsultarDespesasRequest Filtro)
        : IQuery<ResumoDespesasResponse>;
}
