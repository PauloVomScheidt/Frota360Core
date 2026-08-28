namespace Frota360.Domain.Common
{
    /// <summary>
    /// Critérios da consulta à trilha de auditoria. Vira um record em vez de seis parâmetros
    /// opcionais (como em <c>IManutencaoRepository.GetAllAsync</c>) porque já são muitos.
    ///
    /// Note que <c>EmpresaId</c> <b>não está aqui</b>: ele é parâmetro separado do repositório,
    /// para que nenhum caminho consiga montar um filtro sem escopo de empresa.
    /// </summary>
    /// <param name="Pagina">Começa em 1.</param>
    /// <param name="Ate">Inclusivo: o repositório trata como fim do dia.</param>
    public sealed record FiltroLogAuditoria(
        int Pagina,
        int TamanhoPagina,
        string? Entidade = null,
        string? Acao = null,
        int? UsuarioId = null,
        DateTime? De = null,
        DateTime? Ate = null);
}
