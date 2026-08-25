using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;

namespace Frota360.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request);
        Task LogoutAsync(int usuarioId);

        /// <summary>Sempre conclui sem indicar se o e-mail existe; quando existe e está ativo, envia o link de reset.</summary>
        Task EsqueciSenhaAsync(EsqueciSenhaRequest request);

        /// <summary>Troca a senha a partir de um token de reset válido e revoga a sessão ativa. False se inválido/expirado.</summary>
        Task<bool> RedefinirSenhaAsync(RedefinirSenhaRequest request);
    }
}
