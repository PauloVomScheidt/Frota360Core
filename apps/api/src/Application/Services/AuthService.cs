using Frota360.Application.Common;
using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.Services
{
    public class AuthService(IUsuarioRepository repository,
                             ITokenService tokenService,
                             IEmailService emailService,
                             FrontendSettings frontendSettings,
                             ILogger<AuthService> logger) : IAuthService
    {
        private static readonly TimeSpan RefreshTokenValidade = TimeSpan.FromDays(7);
        private static readonly TimeSpan ResetSenhaValidade = TimeSpan.FromMinutes(30);

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

                if (!usuario.Ativo)
                {
                    logger.LogWarning("Tentativa de login de usuário desativado. Id {Id}", usuario.Id);
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
                    Email = usuario.Email,
                    Role = usuario.Role
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
                var usuario = await repository.GetByRefreshTokenHashAsync(TokenHelper.Hash(request.RefreshToken));

                if (usuario is null)
                {
                    logger.LogWarning("Tentativa de refresh com token desconhecido");
                    return null;
                }

                if (usuario.RefreshTokenExpiraEm is null || usuario.RefreshTokenExpiraEm < DateTime.Now)
                {
                    logger.LogWarning("Tentativa de refresh com token expirado. Id {Id}", usuario.Id);
                    return null;
                }

                if (!usuario.Ativo)
                {
                    logger.LogWarning("Tentativa de refresh de usuário desativado. Id {Id}", usuario.Id);
                    return null;
                }

                var refreshToken = await RotacionarRefreshTokenAsync(usuario);

                logger.LogInformation("Token renovado com sucesso. Id {Id}", usuario.Id);

                return new AuthResponse
                {
                    Token = tokenService.GerarToken(usuario),
                    RefreshToken = refreshToken,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    Role = usuario.Role
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

        public async Task EsqueciSenhaAsync(EsqueciSenhaRequest request)
        {
            var usuario = await repository.GetByEmailAsync(request.Email);

            // Resposta sempre neutra: nunca revelamos se o e-mail existe
            if (usuario is null || !usuario.Ativo)
            {
                logger.LogInformation("Pedido de reset para e-mail não elegível");
                return;
            }

            var token = tokenService.GerarRefreshToken();

            usuario.ResetSenhaTokenHash = TokenHelper.Hash(token);
            usuario.ResetSenhaExpiraEm = DateTime.Now.Add(ResetSenhaValidade);
            await repository.UpdateAsync(usuario);

            var link = $"{frontendSettings.BaseUrl.TrimEnd('/')}/redefinir-senha?token={Uri.EscapeDataString(token)}";

            await emailService.EnviarAsync(usuario.Email, "Redefinição de senha — Frota360", CorpoDeEmail.ComLink(
                chamada: "Recebemos um pedido para redefinir a senha da sua conta no Frota360.",
                acao: "Criar uma nova senha (link válido por 30 minutos)",
                link: link,
                aviso: "Se você não fez este pedido, ignore este e-mail — sua senha continua a mesma."));

            logger.LogInformation("E-mail de reset de senha enviado. Id {Id}", usuario.Id);
        }

        public async Task<bool> RedefinirSenhaAsync(RedefinirSenhaRequest request)
        {
            var usuario = await repository.GetByResetSenhaTokenHashAsync(TokenHelper.Hash(request.Token));

            if (usuario is null || usuario.ResetSenhaExpiraEm is null || usuario.ResetSenhaExpiraEm < DateTime.Now)
            {
                logger.LogWarning("Tentativa de redefinição com token inválido ou expirado");
                return false;
            }

            if (!usuario.Ativo)
            {
                logger.LogWarning("Tentativa de redefinição de senha de usuário desativado. Id {Id}", usuario.Id);
                return false;
            }

            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha);
            usuario.ResetSenhaTokenHash = null;
            usuario.ResetSenhaExpiraEm = null;

            // Derruba sessões antigas: quem tiver o refresh token anterior perde o acesso
            usuario.RefreshTokenHash = null;
            usuario.RefreshTokenExpiraEm = null;

            await repository.UpdateAsync(usuario);

            logger.LogInformation("Senha redefinida com sucesso. Id {Id}", usuario.Id);

            return true;
        }

        /// <summary>Gera um novo refresh token, persiste apenas o hash e devolve o valor em claro.</summary>
        private async Task<string> RotacionarRefreshTokenAsync(Usuario usuario)
        {
            var refreshToken = tokenService.GerarRefreshToken();

            usuario.RefreshTokenHash = TokenHelper.Hash(refreshToken);
            usuario.RefreshTokenExpiraEm = DateTime.Now.Add(RefreshTokenValidade);
            await repository.UpdateAsync(usuario);

            return refreshToken;
        }
    }
}
