using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IAbastecimentoRepository
    {
        /// <summary>
        /// Lista da empresa, do mais recente para o mais antigo, com veículo, rota,
        /// motorista e usuário carregados.
        ///
        /// <paramref name="motoristaId"/> é o segundo eixo: quando informado, devolve só os
        /// lançamentos daquele motorista — é o que a tela do motorista usa, e o que sustenta
        /// o gasto por motorista para a gestão. <paramref name="ate"/> é inclusivo.
        /// </summary>
        Task<IEnumerable<Abastecimento>> GetAllAsync(int empresaId, int? veiculoId = null,
            int? motoristaId = null, DateTime? de = null, DateTime? ate = null);

        Task<Abastecimento?> GetByIdAsync(int id, int empresaId);

        /// <summary>
        /// Usado antes de excluir um veículo, que não pode sumir deixando abastecimentos
        /// apontando para um registro que não existe mais (mesma regra da rota, RN08).
        /// </summary>
        Task<bool> ExisteComVeiculoAsync(int empresaId, int veiculoId);

        Task<Abastecimento> AddAsync(Abastecimento abastecimento);
        Task<Abastecimento> UpdateAsync(Abastecimento abastecimento);
        Task DeleteAsync(Abastecimento abastecimento);
    }
}
