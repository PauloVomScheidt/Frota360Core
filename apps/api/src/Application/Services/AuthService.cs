using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.Services
{
    public class AuthService(IUsuarioRepository repository, ITokenService tokenService, ILogger<AuthService> logger) : IAuthService
    {
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                logger.LogInformation("Iniciando cadastro de usuário com email {Email}", request.Email);

                var usuario = new Usuario
                {
                    Nome = request.Nome,
                    Email = request.Email,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
                    DataInclusao = DateTime.UtcNow
                };

                var criado = await repository.AddAsync(usuario);

                logger.LogInformation("Usuário cadastrado com sucesso. Id {Id} | Email {Email}", criado.Id, criado.Email);

                return new AuthResponse
                {
                    Token = tokenService.GerarToken(criado),
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

                logger.LogInformation("Login realizado com sucesso. Id {Id} | Email {Email}", usuario.Id, usuario.Email);

                return new AuthResponse
                {
                    Token = tokenService.GerarToken(usuario),
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
    }
}
