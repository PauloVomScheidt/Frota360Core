using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class UsuarioRepository(Frota360DbContext context) : IUsuarioRepository
    {
        // A normalização fica aqui, e não só em quem chama, para que todo lookup por e-mail
        // passe por ela — inclusive os que forem escritos depois. Ver EmailNormalizado.
        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            var normalizado = Domain.Common.EmailNormalizado.De(email);
            return await context.Usuarios.FirstOrDefaultAsync(u => u.Email == normalizado);
        }

        public async Task<Usuario?> GetByIdAsync(int id)
            => await context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

        public async Task<Usuario?> GetByRefreshTokenHashAsync(string refreshTokenHash)
            => await context.Usuarios.FirstOrDefaultAsync(u => u.RefreshTokenHash == refreshTokenHash);

        public async Task<Usuario?> GetByResetSenhaTokenHashAsync(string resetSenhaTokenHash)
            => await context.Usuarios.FirstOrDefaultAsync(u => u.ResetSenhaTokenHash == resetSenhaTokenHash);

        public async Task<IEnumerable<Usuario>> GetAllByEmpresaAsync(int empresaId)
            => await context.Usuarios.AsNoTracking()
                .Where(u => u.EmpresaId == empresaId)
                .OrderBy(u => u.Nome)
                .ToListAsync();

        public async Task<int> ContarAdminsAtivosAsync(int empresaId)
            => await context.Usuarios.CountAsync(u =>
                u.EmpresaId == empresaId && u.Role == Domain.Common.Roles.Admin && u.Ativo);

        public async Task<bool> ExisteEmailAsync(string email)
        {
            var normalizado = Domain.Common.EmailNormalizado.De(email);
            return await context.Usuarios.AnyAsync(u => u.Email == normalizado);
        }

        public async Task<bool> ExisteCpfNaEmpresaAsync(int empresaId, string cpf, int ignorarUsuarioId)
            => await context.Usuarios.AsNoTracking()
                .AnyAsync(u => u.EmpresaId == empresaId && u.CPF == cpf && u.Id != ignorarUsuarioId);

        public async Task<IEnumerable<Usuario>> GetMotoristasByEmpresaAsync(int empresaId)
            => await context.Usuarios.AsNoTracking()
                .Where(u => u.EmpresaId == empresaId && u.Role == Domain.Common.Roles.Motorista)
                .OrderBy(u => u.Nome)
                .ToListAsync();

        public async Task<Usuario?> GetMotoristaByIdAsync(int id, int empresaId)
            => await context.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id
                                       && u.EmpresaId == empresaId
                                       && u.Role == Domain.Common.Roles.Motorista);

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
