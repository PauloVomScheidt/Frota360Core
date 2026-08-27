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
