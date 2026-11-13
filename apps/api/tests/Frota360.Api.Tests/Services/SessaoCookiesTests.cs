using Frota360.Api.Services;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace Frota360.Api.Tests.Services
{
    public class SessaoCookiesTests
    {
        private static List<string> CookiesEmitidos(Action<HttpResponse> acao)
        {
            var http = new DefaultHttpContext();
            acao(http.Response);
            return http.Response.Headers.SetCookie.Select(c => c!).ToList();
        }

        private static string Cookie(List<string> cookies, string nome) =>
            Assert.Single(cookies, c => c.StartsWith($"{nome}="));

        [Fact]
        public void Emitir_DeveGravarTokenERefreshHttpOnlySecureSameSiteNone()
        {
            var cookies = CookiesEmitidos(r => SessaoCookies.Emitir(r, "jwt-valor", "refresh-valor"));

            Assert.Equal(2, cookies.Count);
            foreach (var (nome, valor) in new[] { (CookiesDeSessao.Token, "jwt-valor"), (CookiesDeSessao.Refresh, "refresh-valor") })
            {
                var cookie = Cookie(cookies, nome);
                Assert.StartsWith($"{nome}={valor};", cookie);
                Assert.Contains("httponly", cookie);
                Assert.Contains("secure", cookie);
                Assert.Contains("samesite=none", cookie);
                Assert.Contains("path=/", cookie);
                Assert.Contains("expires=", cookie);
            }
        }

        [Fact]
        public void Emitir_TokenDeveDurarUmaHoraERefreshSeteDias()
        {
            var cookies = CookiesEmitidos(r => SessaoCookies.Emitir(r, "jwt", "refresh"));

            Assert.InRange(Expiracao(Cookie(cookies, CookiesDeSessao.Token)) - DateTimeOffset.UtcNow,
                TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(61));
            Assert.InRange(Expiracao(Cookie(cookies, CookiesDeSessao.Refresh)) - DateTimeOffset.UtcNow,
                TimeSpan.FromDays(7) - TimeSpan.FromMinutes(1), TimeSpan.FromDays(7) + TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void Limpar_DeveExpirarOsDoisCookiesComOsMesmosAtributosDaEmissao()
        {
            var cookies = CookiesEmitidos(SessaoCookies.Limpar);

            Assert.Equal(2, cookies.Count);
            foreach (var nome in new[] { CookiesDeSessao.Token, CookiesDeSessao.Refresh })
            {
                var cookie = Cookie(cookies, nome);
                Assert.StartsWith($"{nome}=;", cookie);
                Assert.True(Expiracao(cookie) < DateTimeOffset.UtcNow);
                Assert.Contains("httponly", cookie);
                Assert.Contains("secure", cookie);
                Assert.Contains("samesite=none", cookie);
                Assert.Contains("path=/", cookie);
            }
        }

        private static DateTimeOffset Expiracao(string cookie)
        {
            var atributo = cookie.Split(';').Select(p => p.Trim()).Single(p => p.StartsWith("expires="));
            return DateTimeOffset.Parse(atributo["expires=".Length..], System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
