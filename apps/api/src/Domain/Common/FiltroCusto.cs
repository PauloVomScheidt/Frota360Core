using Frota360.Domain.Enums;

namespace Frota360.Domain.Common
{
    /// <summary>
    /// Critérios da consulta de custos, no mesmo molde de <see cref="FiltroLogAuditoria"/>.
    ///
    /// Não carrega paginação de propósito: a lista e o resumo usam o <b>mesmo</b> filtro, e
    /// só a lista pagina — página e tamanho vão como parâmetros à parte do repositório.
    ///
    /// Como no filtro da auditoria, <c>EmpresaId</c> <b>não está aqui</b>: é parâmetro
    /// separado, para que nenhum caminho monte um filtro sem escopo de empresa.
    /// </summary>
    /// <param name="MotoristaId">
    /// Preenchido, descarta a manutenção inteira do resultado: manutenção não é atribuída a
    /// motorista no modelo. A tela avisa o usuário disso.
    /// </param>
    /// <param name="Ate">Inclusivo: o repositório trata como fim do dia.</param>
    public sealed record FiltroCusto(
        int? VeiculoId = null,
        int? MotoristaId = null,
        OrigemCusto? Origem = null,
        DateTime? De = null,
        DateTime? Ate = null);
}
