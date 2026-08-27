using System.Security.Cryptography;
using System.Text;

namespace Frota360.Application.Common
{
    /// <summary>
    /// Hash de tokens opacos (refresh, convite, reset de senha).
    /// Apenas o hash SHA-256 é persistido; o valor em claro vai só para o dono do token.
    /// </summary>
    public static class TokenHelper
    {
        public static string Hash(string token)
            => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
