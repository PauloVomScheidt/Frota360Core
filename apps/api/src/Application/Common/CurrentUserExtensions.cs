using Frota360.Application.Interfaces;
using Frota360.Domain.Common;

namespace Frota360.Application.Common
{
    public static class CurrentUserExtensions
    {
        /// <summary>
        /// Verdadeiro quando quem está na requisição é um motorista. O id dele é o
        /// próprio <c>UsuarioId</c> — não há vínculo nem claim extra a resolver.
        /// </summary>
        public static bool EhMotorista(this ICurrentUserService currentUser)
            => currentUser.Role == Roles.Motorista;
    }
}
