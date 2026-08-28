using Frota360.Application.Interfaces;
using System.Security.Claims;

namespace Frota360.Api.Services
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public int UsuarioId => LerClaimInt(ClaimTypes.NameIdentifier, "sub");

        public int EmpresaId => LerClaimInt("empresaId");

        public string Role =>
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role)
            ?? throw new UnauthorizedAccessException("Token inválido: refaça o login.");

        // Nome e e-mail são identificação para a trilha de auditoria, não autorização:
        // um token sem eles não invalida a requisição, só deixa a linha menos informativa.
        public string Nome => LerClaim(ClaimTypes.Name, "name") ?? string.Empty;

        public string Email => LerClaim(ClaimTypes.Email, "email") ?? string.Empty;

        public string? IpOrigem =>
            httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        private string? LerClaim(params string[] tipos)
        {
            var user = httpContextAccessor.HttpContext?.User;

            foreach (var tipo in tipos)
            {
                var valor = user?.FindFirstValue(tipo);
                if (!string.IsNullOrWhiteSpace(valor))
                    return valor;
            }

            return null;
        }

        private int LerClaimInt(params string[] tipos)
        {
            var user = httpContextAccessor.HttpContext?.User;

            foreach (var tipo in tipos)
            {
                var valor = user?.FindFirstValue(tipo);
                if (int.TryParse(valor, out var id))
                    return id;
            }

            // Token antigo (emitido antes do multi-tenant) ou requisição não autenticada
            throw new UnauthorizedAccessException("Token inválido: refaça o login.");
        }
    }
}
