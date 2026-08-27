using Frota360.Domain.Entities;
using Frota360.Domain.Enums;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IManutencaoRepository
    {
        /// <summary>Lista da empresa, com veículo e tipo carregados, filtrando opcionalmente por veículo e status.</summary>
        Task<IEnumerable<Manutencao>> GetAllAsync(int empresaId, int? veiculoId = null, StatusManutencao? status = null);
        Task<Manutencao?> GetByIdAsync(int id, int empresaId);

        /// <summary>Já existe manutenção pendente do mesmo tipo para o veículo na mesma quilometragem.</summary>
        Task<bool> ExisteDuplicadaAsync(int empresaId, int veiculoId, int tipoManutencaoId, int quilometragemPrevista, int? ignorarId = null);

        /// <summary>Usado antes de excluir um tipo, que não pode sumir deixando manutenções órfãs.</summary>
        Task<bool> ExisteComTipoAsync(int empresaId, int tipoManutencaoId);

        Task<Manutencao> AddAsync(Manutencao manutencao);
        Task<Manutencao> UpdateAsync(Manutencao manutencao);
        Task DeleteAsync(Manutencao manutencao);
    }
}
