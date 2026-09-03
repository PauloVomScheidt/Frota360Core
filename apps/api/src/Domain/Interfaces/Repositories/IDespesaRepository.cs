using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IDespesaRepository
    {
        /// <summary>
        /// Lista da empresa, da mais recente para a mais antiga, com veículo, tipo e
        /// motorista carregados. <paramref name="ate"/> é inclusivo.
        /// </summary>
        Task<IEnumerable<Despesa>> GetAllAsync(int empresaId, int? veiculoId = null,
            int? motoristaId = null, int? tipoDespesaId = null, DateTime? de = null, DateTime? ate = null);

        Task<Despesa?> GetByIdAsync(int id, int empresaId);

        /// <summary>
        /// Usado antes de excluir um veículo, que não pode sumir deixando despesas
        /// apontando para um registro que não existe mais (RN08, terceira guarda).
        /// </summary>
        Task<bool> ExisteComVeiculoAsync(int empresaId, int veiculoId);

        /// <summary>Usado antes de excluir um tipo do catálogo: em uso, ele só pode ser inativado.</summary>
        Task<bool> ExisteComTipoAsync(int empresaId, int tipoDespesaId);

        Task<Despesa> AddAsync(Despesa despesa);
        Task<Despesa> UpdateAsync(Despesa despesa);
        Task DeleteAsync(Despesa despesa);
    }
}
