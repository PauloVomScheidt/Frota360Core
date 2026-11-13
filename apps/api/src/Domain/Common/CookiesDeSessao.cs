namespace Frota360.Domain.Common
{
    /// <summary>
    /// Nomes dos cookies HttpOnly que carregam o JWT e o refresh token — vive no Domain
    /// (sem dependência de ASP.NET) para que a Infrastructure (validação do JWT) e a Api
    /// (controllers de auth) compartilhem o mesmo nome sem uma referenciar a outra.
    /// </summary>
    public static class CookiesDeSessao
    {
        public const string Token = "frota360_token";
        public const string Refresh = "frota360_refresh";
    }
}
