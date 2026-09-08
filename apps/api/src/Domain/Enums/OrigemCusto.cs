namespace Frota360.Domain.Enums
{
    /// <summary>
    /// De onde saiu um custo. Não é persistido em coluna alguma: os custos vivem nas
    /// tabelas de origem (<c>Abastecimento.Valor</c>, <c>Manutencao.Custo</c> e
    /// <c>Despesa.Valor</c>) e este enum é o discriminador que a leitura usa para uni-los.
    ///
    /// <c>Despesa</c> foi a primeira origem acrescentada depois do desenho inicial, e
    /// confirmou o que ele comprava: o DTO de custo não mudou, só ganhou mais um valor
    /// aceito. Ver a seção Custos de docs/contexto-api.md.
    /// </summary>
    public enum OrigemCusto
    {
        Abastecimento = 0,
        Manutencao = 1,

        /// <summary>Custo avulso — pedágio, multa, IPVA, seguro. Única origem cuja tabela é fonte de verdade.</summary>
        Despesa = 2
    }
}
