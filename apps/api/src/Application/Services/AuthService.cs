using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Frota360.Application.Services
{
    public class AuthService(IUsuarioRepository repository, ITokenService tokenService, ILogger<AuthService> logger) : IAuthService
    {
        private static readonly TimeSpan RefreshTokenValidade = TimeSpan.FromDays(7);

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                logger.LogInformation("Iniciando cadastro de usuário com email {Email}", request.Email);

                var refreshToken = tokenService.GerarRefreshToken();

                var usuario = new Usuario
                {
                    Nome = request.Nome,
                    Email = request.Email,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
                    RefreshTokenHash = HashRefreshToken(refreshToken),
                    RefreshTokenExpiraEm = DateTime.UtcNow.Add(RefreshTokenValidade),
                    DataInclusao = DateTime.UtcNow
                };

                var criado = await repository.AddAsync(usuario);

                logger.LogInformation("Usuário cadastrado com sucesso. Id {Id} | Email {Email}", criado.Id, criado.Email);

                return new AuthResponse
                {
                    Token = tokenService.GerarToken(criado),
                    RefreshToken = refreshToken,
                    Nome = criado.Nome,
                    Email = criado.Email
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao cadastrar usuário com email {Email}", request.Email);
                throw;
            }
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                logger.LogInformation("Tentativa de login para o email {Email}", request.Email);

                var usuario = await repository.GetByEmailAsync(request.Email);

                if (usuario is null)
                {
                    logger.LogWarning("Tentativa de login com usuário inexistente. Email {Email}", request.Email);
                    return null;
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
                {
                    logger.LogWarning("Tentativa de login com senha inválida. Email {Email}", request.Email);
                    return null;
                }

                var refreshToken = await RotacionarRefreshTokenAsync(usuario);

                logger.LogInformation("Login realizado com sucesso. Id {Id} | Email {Email}", usuario.Id, usuario.Email);

                return new AuthResponse
                {
                    Token = tokenService.GerarToken(usuario),
                    RefreshToken = refreshToken,
                    Nome = usuario.Nome,
                    Email = usuario.Email
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro durante login do usuário {Email}", request.Email);
                throw;
            }
        }

        public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request)
        {
            try
            {
                var usuario = await repository.GetByRefreshTokenHashAsync(HashRefreshToken(request.RefreshToken));

                if (usuario is null)
                {
                    logger.LogWarning("Tentativa de refresh com token desconhecido");
                    return null;
                }

                if (usuario.RefreshTokenExpiraEm is null || usuario.RefreshTokenExpiraEm < DateTime.UtcNow)
                {
                    logger.LogWarning("Tentativa de refresh com token expirado. Id {Id}", usuario.Id);
                    return null;
                }

                var refreshToken = await RotacionarRefreshTokenAsync(usuario);

                logger.LogInformation("Token renovado com sucesso. Id {Id}", usuario.Id);

                return new AuthResponse
                {
                    Token = tokenService.GerarToken(usuario),
                    RefreshToken = refreshToken,
                    Nome = usuario.Nome,
                    Email = usuario.Email
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao renovar token");
                throw;
            }
        }

        public async Task LogoutAsync(int usuarioId)
        {
            var usuario = await repository.GetByIdAsync(usuarioId);

            if (usuario is null)
                return;

            usuario.RefreshTokenHash = null;
            usuario.RefreshTokenExpiraEm = null;
            await repository.UpdateAsync(usuario);

            logger.LogInformation("Logout realizado. Id {Id}", usuarioId);
        }

        /// <summary>Gera um novo refresh token, persiste apenas o hash e devolve o valor em claro.</summary>
        private async Task<string> RotacionarRefreshTokenAsync(Usuario usuario)
        {
            var refreshToken = tokenService.GerarRefreshToken();

            usuario.RefreshTokenHash = HashRefreshToken(refreshToken);
            usuario.RefreshTokenExpiraEm = DateTime.UtcNow.Add(RefreshTokenValidade);
            await repository.UpdateAsync(usuario);

            return refreshToken;
        }

        private static string HashRefreshToken(string refreshToken)
            => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}
