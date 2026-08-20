using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class UsuarioRepository(Frota360DbContext context) : IUsuarioRepository
    {
        public async Task<Usuario?> GetByEmailAsync(string email)
            => await context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<Usuario?> GetByIdAsync(int id)
            => await context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

        public async Task<Usuario?> GetByRefreshTokenHashAsync(string refreshTokenHash)
            => await context.Usuarios.FirstOrDefaultAsync(u => u.RefreshTokenHash == refreshTokenHash);

        public async Task<bool> ExisteEmailAsync(string email)
            => await context.Usuarios.AnyAsync(u => u.Email == email);

        public async Task<Usuario> AddAsync(Usuario usuario)
        {
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();
            return usuario;
        }

        public async Task<Usuario> UpdateAsync(Usuario usuario)
        {
            context.Usuarios.Update(usuario);
            await context.SaveChangesAsync();
            return usuario;
        }
    }
}
