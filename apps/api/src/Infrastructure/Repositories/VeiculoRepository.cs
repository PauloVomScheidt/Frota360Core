using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class VeiculoRepository(Frota360DbContext context) : IVeiculoRepository
    {
        public async Task<IEnumerable<Veiculo>> GetAllAsync()
            => await context.Veiculos.AsNoTracking().ToListAsync();
    }
}
