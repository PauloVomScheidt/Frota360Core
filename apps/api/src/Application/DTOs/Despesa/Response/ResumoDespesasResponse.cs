namespace Frota360.Application.DTOs.Despesa.Response
{
    /// <summary>
    /// Os dois números do rodapé de <c>/despesas</c>, sempre do <b>filtro inteiro</b> e não da
    /// página exibida. Mesma forma do resumo de abastecimento.
    /// </summary>
    public class ResumoDespesasResponse
    {
        public int Quantidade { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
