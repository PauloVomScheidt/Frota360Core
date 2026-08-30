using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IRotaRepository
    {
        Task<IEnumerable<Rota>> GetAllAsync(int empresaId);

        /// <summary>Rotas de um único motorista — base da tela "Minhas rotas".</summary>
        Task<IEnumerable<Rota>> GetAllByMotoristaAsync(int empresaId, int motoristaId);

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
