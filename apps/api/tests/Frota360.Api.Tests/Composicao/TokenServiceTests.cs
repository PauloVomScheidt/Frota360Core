using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Frota360.Api.Tests.Composicao
{
    /// <summary>
    /// Fica aqui, e não nos unitários, porque o que importa provar é a volta completa: o token
    /// emitido pelo TokenService passa na validação que o AddInfrastructure configura no JwtBearer.
    /// </summary>
    public class TokenServiceTests
    {
        private readonly ServiceProvider _provedor = Composicao.Provedor();

        private ITokenService TokenService() => TokenService(_provedor);

        private static ITokenService TokenService(ServiceProvider provedor) =>
            provedor.CreateScope().ServiceProvider.GetRequiredService<ITokenService>();

        private TokenValidationParameters ParametrosDoJwtBearer() =>
            _provedor.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;

        private static Usuario NovoUsuario() => new()
        {
            Id = 42,
            EmpresaId = 7,
            Nome = "Maria Souza",
            Email = "maria@exemplo.com",
            Role = "Motorista"
        };

        [Fact]
        public void GerarToken_DevePassarNaValidacaoDoJwtBearerComAsClaimsDaSessao()
        {
            var token = TokenService().GerarToken(NovoUsuario());

            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }
                .ValidateToken(token, ParametrosDoJwtBearer(), out _);

            Assert.Equal("42", principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
            Assert.Equal("7", principal.FindFirstValue("empresaId"));
            Assert.Equal("maria@exemplo.com", principal.FindFirstValue(JwtRegisteredClaimNames.Email));
            Assert.Equal("Maria Souza", principal.FindFirstValue(JwtRegisteredClaimNames.Name));
            Assert.Equal("Motorista", principal.FindFirstValue(ClaimTypes.Role));
            Assert.False(string.IsNullOrEmpty(principal.FindFirstValue(JwtRegisteredClaimNames.Jti)));
        }

        [Fact]
        public void GerarToken_DeveExpirarEmUmaHoraEmUtc()
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(TokenService().GerarToken(NovoUsuario()));

            Assert.Equal(Composicao.Emissor, jwt.Issuer);
            Assert.Contains(Composicao.Audiencia, jwt.Audiences);
            Assert.InRange(jwt.ValidTo, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
        }

        [Fact]
        public void GerarToken_AssinadoComOutraChave_DeveSerRecusado()
        {
            var valores = Composicao.ConfiguracaoMinima();
            valores["Jwt:Key"] = "outra-chave-tambem-com-mais-de-32-caracteres";
            using var outroProvedor = Composicao.Provedor(valores);

            var tokenDeOutraChave = TokenService(outroProvedor).GerarToken(NovoUsuario());

            Assert.ThrowsAny<SecurityTokenException>(() =>
                new JwtSecurityTokenHandler().ValidateToken(tokenDeOutraChave, ParametrosDoJwtBearer(), out _));
        }

        [Fact]
        public void GerarRefreshToken_DeveTer64BytesAleatorios()
        {
            var primeiro = TokenService().GerarRefreshToken();
            var segundo = TokenService().GerarRefreshToken();

            Assert.Equal(64, Convert.FromBase64String(primeiro).Length);
            Assert.NotEqual(primeiro, segundo);
        }
    }
}
