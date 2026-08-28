using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class RotaRepository(Frota360DbContext context) : IRotaRepository
    {
        public async Task<IEnumerable<Rota>> GetAllAsync(int empresaId)
            => await context.Rotas.AsNoTracking()
                .Include(r => r.Motorista)
                .Where(r => r.EmpresaId == empresaId)
                .ToListAsync();

        public async Task<IEnumerable<Rota>> GetAllByMotoristaAsync(int empresaId, int motoristaId)
            => await context.Rotas.AsNoTracking()
                .Include(r => r.Motorista)
                .Where(r => r.EmpresaId == empresaId && r.CodigoMotorista == motoristaId)
                // A tela do motorista mostra a rota ativa primeiro e o histórico do mais
                // recente para o mais antigo.
                .OrderByDescending(r => r.DataInicio)
                .ToListAsync();

        public async Task<bool> ExisteComVeiculoAsync(int empresaId, int veiculoId)
            => await context.Rotas.AsNoTracking()
                .AnyAsync(r => r.EmpresaId == empresaId && r.CodigoVeiculo == veiculoId);

        // Rastreado (sem AsNoTracking): serve tanto para leitura quanto para update.
        public async Task<Rota?> GetByIdAsync(int id, int empresaId)
            => await context.Rotas
                .Include(r => r.Motorista)
                .FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == empresaId);

        public async Task<Rota> AddAsync(Rota rota)
        {
            context.Rotas.Add(rota);
            await context.SaveChangesAsync();
            return rota;
        }

        public async Task DeleteAsync(Rota rota)
        {
            context.Rotas.Remove(rota);
            await context.SaveChangesAsync();
        }

        public async Task<Rota> UpdateAsync(Rota rota)
        {
            context.Rotas.Update(rota);
            await context.SaveChangesAsync();
            return rota;
        }
    }
}
