namespace Frota360.Application.DTOs.Abastecimento.Response
{
    /// <summary>
    /// Os dois números do rodapé de <c>/abastecimentos</c>, sempre do <b>filtro inteiro</b> e
    /// não da página exibida — virar de página não pode mexer no que a tela diz que foi gasto.
    ///
    /// Nasceu com a paginação do servidor: antes o front somava o array que recebia, o que só
    /// funcionava enquanto a lista inteira vinha numa requisição.
    /// </summary>
    public class ResumoAbastecimentosResponse
    {
        public int Quantidade { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
