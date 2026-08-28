namespace Frota360.Application.Common
{
    /// <summary>
    /// Um campo que mudou, no formato gravado em <c>LogAuditoria.Alteracoes</c> e devolvido
    /// ao front. Os valores são texto já formatado — o log é histórico legível, não um dump
    /// tipado para reprocessamento.
    /// </summary>
    public sealed record AlteracaoCampo(string Campo, string? De, string? Para);
}
