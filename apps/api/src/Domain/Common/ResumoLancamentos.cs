namespace Frota360.Domain.Common
{
    /// <summary>
    /// Os dois números do rodapé de uma listagem de lançamentos: quantos são e quanto somam,
    /// sempre do <b>filtro inteiro</b> e não da página exibida.
    ///
    /// Existe porque a paginação passou a ser do servidor: antes o front somava o array que
    /// recebia, o que só funcionava enquanto a lista inteira vinha numa requisição. Agora o
    /// <c>COUNT</c> e o <c>SUM</c> saem do banco, o que também é mais barato do que trazer
    /// linhas para somar no cliente.
    ///
    /// Serve abastecimento e despesa — os dois têm exatamente esta forma de rodapé.
    /// </summary>
    public sealed record ResumoLancamentos(int Quantidade, decimal ValorTotal);
}
