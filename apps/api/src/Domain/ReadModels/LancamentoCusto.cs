using Frota360.Domain.Enums;

namespace Frota360.Domain.ReadModels
{
    /// <summary>
    /// Uma linha de custo, já normalizada entre as origens. Não é entidade — não tem tabela
    /// nem chave própria: é a projeção que o <c>CustoRepository</c> produz unindo
    /// <c>Abastecimento</c> e <c>Manutencao</c>.
    /// </summary>
    /// <param name="OrigemId">Id na tabela de origem — é por ele que a tela volta ao registro.</param>
    /// <param name="MotoristaId">Nulo em manutenção, que não é atribuída a motorista.</param>
    /// <param name="Categoria">"Combustível" no abastecimento; o nome do tipo na manutenção.</param>
    public sealed record LancamentoCusto(
        OrigemCusto Origem,
        int OrigemId,
        DateTime Data,
        int VeiculoId,
        string VeiculoNome,
        string VeiculoPlaca,
        int? MotoristaId,
        string? MotoristaNome,
        string Categoria,
        decimal Valor,
        string? Observacao);
}
