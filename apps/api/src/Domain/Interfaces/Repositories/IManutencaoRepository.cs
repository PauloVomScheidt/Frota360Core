using Frota360.Domain.Entities;
using Frota360.Domain.Enums;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IManutencaoRepository
    {
        /// <summary>
        /// Lista da empresa, com veículo e tipo carregados, filtrando opcionalmente por veículo,
        /// status e período.
        ///
        /// O período é aplicado sobre a <b>data relevante do status</b>: pendência é situada
        /// pela <c>DataPrevista</c>, manutenção feita pela <c>DataRealizacao</c>. <c>ate</c> é
        /// inclusivo (o repositório estende até o fim do dia).
        /// </summary>
        Task<IEnumerable<Manutencao>> GetAllAsync(int empresaId, int? veiculoId = null,
            StatusManutencao? status = null, DateTime? de = null, DateTime? ate = null);
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
