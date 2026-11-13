using Frota360.Domain.Common;

namespace Frota360.Api.Services
{
    /// <summary>
    /// Emite e limpa os cookies HttpOnly que carregam o JWT e o refresh token. Nenhum dos
    /// dois chega ao corpo da resposta nem a localStorage/sessionStorage do front — só o
    /// navegador os guarda, e só o servidor os lê, via header Cookie automático (mitiga
    /// exfiltração por XSS, que consegue ler qualquer web storage mas não um cookie HttpOnly).
    /// </summary>
    public static class SessaoCookies
    {
        public static void Emitir(HttpResponse response, string token, string refreshToken)
        {
            response.Cookies.Append(CookiesDeSessao.Token, token, Opcoes(TimeSpan.FromHours(1)));
            response.Cookies.Append(CookiesDeSessao.Refresh, refreshToken, Opcoes(TimeSpan.FromDays(7)));
        }

        public static void Limpar(HttpResponse response)
        {
            response.Cookies.Delete(CookiesDeSessao.Token, OpcoesDeExclusao());
            response.Cookies.Delete(CookiesDeSessao.Refresh, OpcoesDeExclusao());
        }

        // SameSite=None: front e API vivem em origens diferentes (portas em dev, domínios em
        // produção atrás do CloudFront/Caddy) — sem isso o navegador descarta o cookie em toda
        // requisição cross-site. SameSite=None exige Secure por regra do próprio navegador,
        // e as duas API bindings de dev (7271/5062) e a de produção já falam HTTPS.
        private static CookieOptions Opcoes(TimeSpan duracao) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.Add(duracao),
            Path = "/",
        };

        // Mesmos atributos da emissão, sem Expires: Cookies.Delete só remove um cookie que o
        // navegador reconhece como o mesmo (path e flags precisam bater com a emissão).
        private static CookieOptions OpcoesDeExclusao() => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
        };
    }
}
