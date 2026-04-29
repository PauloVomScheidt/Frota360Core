using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.Interfaces.Services;

namespace Frota360.Application.Services
{
    public class AuthService(IUsuarioRepository repository, ITokenService tokenService) : IAuthService
    {
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var usuario = new Usuario
            {
                Nome = request.Nome,
                Email = request.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
                DataInclusao = DateTime.UtcNow
            };

            var criado = await repository.AddAsync(usuario);

            return new AuthResponse
            {
                Token = tokenService.GerarToken(criado),
                Nome = criado.Nome,
                Email = criado.Email
            };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var usuario = await repository.GetByEmailAsync(request.Email);

            if (usuario is null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
                return null;

            return new AuthResponse
            {
                Token = tokenService.GerarToken(usuario),
                Nome = usuario.Nome,
                Email = usuario.Email
            };
        }
    }
}
