using Frota360.Domain.Common;
using Frota360.Domain.ReadModels;

namespace Frota360.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Leitura consolidada dos custos da empresa. É um repositório de <b>read model</b>, e por
    /// isso atravessa três tabelas (<c>Abastecimento</c>, <c>Manutencao</c> e <c>Rota</c>) em
    /// vez de guardar um agregado — exceção deliberada ao "um repositório por agregado".
    ///
    /// Não existe tabela de custos: o custo continua morando na tabela de origem, e aqui ele
    /// é apenas unido na leitura. Nada neste contrato escreve.
    /// </summary>
    public interface ICustoRepository
    {
        /// <summary>
        /// Página de lançamentos, mais recentes primeiro, com o total que satisfaz o filtro
        /// (ignorando a paginação) para o rodapé da tela.
        /// </summary>
        Task<(IEnumerable<LancamentoCusto> Itens, int Total)> ConsultarAsync(
            int empresaId, FiltroCusto filtro, int pagina, int tamanhoPagina);

        /// <summary>Uma linha por (veículo × origem) — quem pivota é o handler.</summary>
        Task<IEnumerable<TotalCustoPorVeiculo>> SomarPorVeiculoAsync(int empresaId, FiltroCusto filtro);

        /// <summary>Uma linha por (ano, mês × origem) — quem pivota é o handler.</summary>
        Task<IEnumerable<TotalCustoPorMes>> SomarPorMesAsync(int empresaId, FiltroCusto filtro);

        /// <summary>
        /// Manutenções concluídas no período cujo custo não foi informado. Elas ficam de fora
        /// de toda soma, então a tela precisa dizer quantas são — senão o total mente por omissão.
        /// </summary>
        Task<int> ContarManutencoesSemCustoAsync(int empresaId, FiltroCusto filtro);

        /// <summary>Km apurado por veículo no período — denominador do custo por km.</summary>
        Task<IEnumerable<KmPorVeiculo>> SomarKmPorVeiculoAsync(int empresaId, FiltroCusto filtro);
    }
}
