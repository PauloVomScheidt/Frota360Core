namespace Frota360.Domain.Common
{
    /// <summary>
    /// Critérios da consulta de despesas, no molde de <see cref="FiltroAbastecimento"/>.
    ///
    /// Como nos demais filtros, <c>EmpresaId</c> <b>não está aqui</b>: é parâmetro separado do
    /// repositório, para que nenhum caminho consiga montar um filtro sem escopo de empresa.
    ///
    /// Diferente do abastecimento, a despesa não tem segundo eixo — `/despesas` é tela de gestão,
    /// e o motorista sequer a enxerga. <see cref="MotoristaId"/> aqui é filtro de relatório.
    /// </summary>
    /// <param name="Pagina">Começa em 1.</param>
    /// <param name="Ate">Inclusivo: o repositório estende até o fim do dia.</param>
    public sealed record FiltroDespesa(
        int Pagina,
        int TamanhoPagina,
        int? VeiculoId = null,
        int? MotoristaId = null,
        int? TipoDespesaId = null,
        DateTime? De = null,
        DateTime? Ate = null);
}
