namespace Frota360.Domain.Common
{
    /// <summary>
    /// Critérios da consulta de abastecimentos, no molde de <see cref="FiltroLogAuditoria"/>.
    ///
    /// Como lá, <c>EmpresaId</c> <b>não está aqui</b>: é parâmetro separado do repositório, para
    /// que nenhum caminho consiga montar um filtro sem escopo de empresa.
    ///
    /// ⚠️ <see cref="MotoristaId"/> é o segundo eixo. Para a role Motorista quem o preenche é o
    /// handler, com o usuário do token — e ele precisa estar aqui, e não ser aplicado depois, para
    /// que o <c>COUNT</c> da paginação também saia recortado. Contado sobre a empresa inteira, o
    /// total do rodapé entregaria ao motorista o volume da frota.
    /// </summary>
    /// <param name="Pagina">Começa em 1.</param>
    /// <param name="Ate">Inclusivo: o repositório estende até o fim do dia.</param>
    public sealed record FiltroAbastecimento(
        int Pagina,
        int TamanhoPagina,
        int? VeiculoId = null,
        int? MotoristaId = null,
        DateTime? De = null,
        DateTime? Ate = null);
}
