namespace Frota360.Domain.Common
{
    /// <summary>
    /// Critérios da consulta de rotas, no molde de <see cref="FiltroAbastecimento"/>.
    /// <c>EmpresaId</c> é parâmetro separado do repositório, como nos demais filtros.
    ///
    /// <see cref="Ativo"/> nasceu com a paginação: antes a tela baixava a lista inteira e fazia
    /// <c>find(r => r.ativo)</c> no cliente — o que deixa de funcionar quando só uma página volta.
    /// Como o filtro também alimenta o <c>Total</c> do resultado, ele serve de contagem de rotas
    /// abertas para o dashboard sem endpoint dedicado.
    ///
    /// ⚠️ <c>MotoristaId</c> não está aqui: o recorte do motorista é do método
    /// <c>ConsultarDoMotoristaAsync</c>, que o recebe à parte — como <c>EmpresaId</c>, para que
    /// nenhum caminho consiga esquecê-lo.
    /// </summary>
    /// <param name="Pagina">Começa em 1.</param>
    public sealed record FiltroRota(
        int Pagina,
        int TamanhoPagina,
        bool? Ativo = null);
}
