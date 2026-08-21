using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;

namespace Frota360.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request);
        Task LogoutAsync(int usuarioId);
    }
}
