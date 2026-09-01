namespace Frota360.Domain.Enums
{
    /// <summary>
    /// De onde saiu um custo. Não é persistido em coluna alguma: os custos vivem nas
    /// tabelas de origem (<c>Abastecimento.Valor</c> e <c>Manutencao.Custo</c>) e este enum
    /// é o discriminador que a leitura usa para uni-los.
    ///
    /// Quando entrar um custo que não tem tela própria (pedágio, multa, IPVA, seguro),
    /// ele vira uma origem nova aqui — e nada muda no contrato do front além de mais um
    /// valor aceito. Ver "Evolução prevista" na seção Custos de docs/contexto-api.md.
    /// </summary>
    public enum OrigemCusto
    {
        Abastecimento = 0,
        Manutencao = 1
    }
}
