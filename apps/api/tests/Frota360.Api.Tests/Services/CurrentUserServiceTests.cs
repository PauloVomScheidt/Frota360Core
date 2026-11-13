using System.Net;
using System.Security.Claims;
using Frota360.Api.Services;
using Microsoft.AspNetCore.Http;

namespace Frota360.Api.Tests.Services
{
    public class CurrentUserServiceTests
    {
        private static CurrentUserService Servico(params Claim[] claims)
        {
            var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")) };
            http.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");
            return new CurrentUserService(new HttpContextAccessor { HttpContext = http });
        }

        private static CurrentUserService SemRequisicao() => new(new HttpContextAccessor());

        [Fact]
        public void Propriedades_ComClaimsDoAspNet_DevemLerOToken()
        {
            var servico = Servico(
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim("empresaId", "7"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, "Maria Souza"),
                new Claim(ClaimTypes.Email, "maria@exemplo.com"));

            Assert.Equal(42, servico.UsuarioId);
            Assert.Equal(7, servico.EmpresaId);
            Assert.Equal("Admin", servico.Role);
            Assert.Equal("Maria Souza", servico.Nome);
            Assert.Equal("maria@exemplo.com", servico.Email);
            Assert.Equal("203.0.113.9", servico.IpOrigem);
        }

        [Fact]
        public void Propriedades_ComClaimsCurtasDoJwt_DevemCairNoSegundoNome()
        {
            var servico = Servico(
                new Claim("sub", "42"),
                new Claim("name", "Maria Souza"),
                new Claim("email", "maria@exemplo.com"));

            Assert.Equal(42, servico.UsuarioId);
            Assert.Equal("Maria Souza", servico.Nome);
            Assert.Equal("maria@exemplo.com", servico.Email);
        }

        [Fact]
        public void EmpresaId_TokenAnteriorAoMultiTenant_DeveRecusarComoNaoAutenticado()
        {
            var servico = Servico(new Claim("sub", "42"));

            var erro = Assert.Throws<UnauthorizedAccessException>(() => servico.EmpresaId);

            Assert.Equal("Token inválido: refaça o login.", erro.Message);
        }

        [Fact]
        public void UsuarioId_ComValorNaoNumerico_DeveRecusar()
        {
            Assert.Throws<UnauthorizedAccessException>(() => Servico(new Claim("sub", "abc")).UsuarioId);
        }

        [Fact]
        public void Role_SemClaim_DeveRecusar()
        {
            Assert.Throws<UnauthorizedAccessException>(() => Servico().Role);
        }

        [Fact]
        public void NomeEEmail_SemClaim_DevemVoltarVaziosSemDerrubarARequisicao()
        {
            var servico = Servico(new Claim("name", "  "));

            Assert.Equal(string.Empty, servico.Nome);
            Assert.Equal(string.Empty, servico.Email);
        }

        [Fact]
        public void ForaDeUmaRequisicao_DeveRecusarIdentidadeEDevolverIpNulo()
        {
            var servico = SemRequisicao();

            Assert.Throws<UnauthorizedAccessException>(() => servico.UsuarioId);
            Assert.Throws<UnauthorizedAccessException>(() => servico.Role);
            Assert.Equal(string.Empty, servico.Nome);
            Assert.Null(servico.IpOrigem);
        }
    }
}
