using Frota360.Domain.Enums;

namespace Frota360.Domain.Common
{
    /// <summary>
    /// Critérios da consulta de manutenções, no molde de <see cref="FiltroAbastecimento"/>.
    /// <c>EmpresaId</c> é parâmetro separado do repositório, como nos demais filtros.
    ///
    /// O período incide sobre a <b>data relevante do status</b>: prazo quando pendente, execução
    /// quando realizada — a regra vive no repositório.
    /// </summary>
    /// <param name="Pagina">Começa em 1.</param>
    /// <param name="Ate">Inclusivo: o repositório estende até o fim do dia.</param>
    public sealed record FiltroManutencao(
        int Pagina,
        int TamanhoPagina,
        int? VeiculoId = null,
        StatusManutencao? Status = null,
        DateTime? De = null,
        DateTime? Ate = null);
}
