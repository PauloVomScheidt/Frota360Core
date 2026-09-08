using Frota360.Domain.Common;
using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IRotaRepository
    {
        /// <summary>
        /// Uma página das rotas da empresa, da mais recente para a mais antiga, com motorista e
        /// <b>veículo</b> carregados. Devolve também o total que satisfaz o filtro.
        ///
        /// ⚠️ A ordenação precisa ser <b>total</b> (data + id). O <c>GetAllAsync</c> que este
        /// método substituiu não tinha <c>OrderBy</c> nenhum — a ordem era a que o banco
        /// quisesse, o que já era frágil e com paginação viraria linha repetida entre páginas.
        /// </summary>
        Task<(IEnumerable<Rota> Itens, int Total)> ConsultarAsync(int empresaId, FiltroRota filtro);

        /// <summary>
        /// Rotas de um único motorista — base da tela "Minhas rotas". O <paramref name="motoristaId"/>
        /// é parâmetro separado, e não campo do filtro, para que nenhum caminho consiga consultar
        /// sem o recorte.
        /// </summary>
        Task<(IEnumerable<Rota> Itens, int Total)> ConsultarDoMotoristaAsync(int empresaId, int motoristaId, FiltroRota filtro);

        /// <summary>
        /// A rota <b>aberta</b> do motorista, ou nulo. "Aberta" é <c>Ativo &amp;&amp; DataFim is null</c> —
        /// mais estrito que o <c>Ativo</c> do filtro de listagem, e é esta a derivação que o
        /// lançamento de abastecimento usa para vincular a viagem.
        ///
        /// Existe para o handler não precisar baixar o histórico inteiro do motorista só para
        /// achar uma linha. Nada impede duas rotas abertas no mesmo veículo (ver
        /// <c>GetVeiculosEmRotaAsync</c>), então devolve a mais recente.
        /// </summary>
        Task<Rota?> GetRotaAbertaDoMotoristaAsync(int empresaId, int motoristaId);

        /// <summary>
        /// Quantidade e km somado das rotas <b>encerradas</b> no período (recorte por
        /// <c>DataFim</c>). Alimenta o KPI "Km da frota" do dashboard, que antes somava
        /// `kmPercorrido` da lista inteira no cliente.
        ///
        /// <c>KmPercorrido</c> é persistido pela API no encerramento — nunca recalculado a
        /// partir de kmInicial/kmFinal.
        /// </summary>
        Task<ResumoRotas> ResumirEncerradasAsync(int empresaId, DateTime de, DateTime ate);

        /// <summary>
        /// Usado antes de excluir um veículo (RN08), que não pode sumir deixando rotas
        /// apontando para um registro que não existe mais. <b>Ignora o estado da rota</b> de
        /// propósito: uma rota encerrada continua sendo histórico que aponta para o veículo.
        /// Para saber se o veículo está rodando agora, use os dois métodos abaixo.
        /// </summary>
        Task<bool> ExisteComVeiculoAsync(int empresaId, int veiculoId);

        /// <summary>
        /// Ids dos veículos com rota aberta. Uma consulta para a listagem inteira — é o que
        /// alimenta <c>VeiculoResponse.EmRota</c> sem N+1.
        /// </summary>
        Task<IReadOnlyCollection<int>> GetVeiculosEmRotaAsync(int empresaId);

        /// <summary>O mesmo, para um veículo só: leitura e correção de um registro.</summary>
        Task<bool> ExisteRotaAtivaComVeiculoAsync(int empresaId, int veiculoId);

        Task<Rota> AddAsync(Rota rota);
        Task<Rota?> GetByIdAsync(int id, int empresaId);
        Task<Rota> UpdateAsync(Rota rota);
        Task DeleteAsync(Rota rota);
    }
}
