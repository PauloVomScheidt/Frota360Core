using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.Services;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Security.Cryptography;
using System.Text;

namespace Frota360.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly IUsuarioRepository _repository = Substitute.For<IUsuarioRepository>();
        private readonly ITokenService _tokenService = Substitute.For<ITokenService>();

        private AuthService CriarServico() =>
            new(_repository, _tokenService, NullLogger<AuthService>.Instance);

        private static string HashDe(string refreshToken) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        [Fact]
        public async Task Register_DeveHashearSenha_PersistirERetornarToken()
        {
            _repository.AddAsync(Arg.Any<Usuario>())
                .Returns(ci =>
                {
                    var u = ci.Arg<Usuario>();
                    u.Id = 1;
                    return u;
                });
            _tokenService.GerarToken(Arg.Any<Usuario>()).Returns("token-jwt");
            _tokenService.GerarRefreshToken().Returns("refresh-token");

            var service = CriarServico();
            var request = new RegisterRequest
            {
                Nome = "Ana",
                Email = "ana@email.com",
                Senha = "SenhaForte123"
            };

            var resposta = await service.RegisterAsync(request);

            Assert.Equal("token-jwt", resposta.Token);
            Assert.Equal("refresh-token", resposta.RefreshToken);
            Assert.Equal("Ana", resposta.Nome);
            Assert.Equal("ana@email.com", resposta.Email);
            // A senha nunca deve ser persistida em texto puro
            await _repository.Received(1).AddAsync(Arg.Is<Usuario>(u =>
                u.SenhaHash != "SenhaForte123" &&
                BCrypt.Net.BCrypt.Verify("SenhaForte123", u.SenhaHash)));
        }

        [Fact]
        public async Task Login_ComCredenciaisValidas_DeveRetornarToken()
        {
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Ana",
                Email = "ana@email.com",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("SenhaForte123")
            };
            _repository.GetByEmailAsync("ana@email.com").Returns(usuario);
            _tokenService.GerarToken(usuario).Returns("token-jwt");
            _tokenService.GerarRefreshToken().Returns("refresh-token");

            var service = CriarServico();

            var resposta = await service.LoginAsync(
                new LoginRequest { Email = "ana@email.com", Senha = "SenhaForte123" });

            Assert.NotNull(resposta);
            Assert.Equal("token-jwt", resposta!.Token);
            Assert.Equal("refresh-token", resposta.RefreshToken);
            // Apenas o hash do refresh token deve ser persistido
            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                u.RefreshTokenHash == HashDe("refresh-token") &&
                u.RefreshTokenExpiraEm > DateTime.UtcNow));
        }

        [Fact]
        public async Task Login_UsuarioInexistente_DeveRetornarNull()
        {
            _repository.GetByEmailAsync(Arg.Any<string>()).Returns((Usuario?)null);

            var service = CriarServico();

            var resposta = await service.LoginAsync(
                new LoginRequest { Email = "naoexiste@email.com", Senha = "qualquer" });

            Assert.Null(resposta);
            _tokenService.DidNotReceive().GerarToken(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task Login_SenhaIncorreta_DeveRetornarNull()
        {
            var usuario = new Usuario
            {
                Email = "ana@email.com",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("SenhaCorreta")
            };
            _repository.GetByEmailAsync("ana@email.com").Returns(usuario);

            var service = CriarServico();

            var resposta = await service.LoginAsync(
                new LoginRequest { Email = "ana@email.com", Senha = "SenhaErrada" });

            Assert.Null(resposta);
            _tokenService.DidNotReceive().GerarToken(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task Refresh_ComTokenValido_DeveRotacionarERetornarNovoPar()
        {
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Ana",
                Email = "ana@email.com",
                RefreshTokenHash = HashDe("refresh-antigo"),
                RefreshTokenExpiraEm = DateTime.UtcNow.AddDays(1)
            };
            _repository.GetByRefreshTokenHashAsync(HashDe("refresh-antigo")).Returns(usuario);
            _tokenService.GerarToken(usuario).Returns("token-novo");
            _tokenService.GerarRefreshToken().Returns("refresh-novo");

            var service = CriarServico();

            var resposta = await service.RefreshAsync(
                new RefreshTokenRequest { RefreshToken = "refresh-antigo" });

            Assert.NotNull(resposta);
            Assert.Equal("token-novo", resposta!.Token);
            Assert.Equal("refresh-novo", resposta.RefreshToken);
            // Rotação: o hash antigo deve ser substituído pelo novo
            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                u.RefreshTokenHash == HashDe("refresh-novo")));
        }

        [Fact]
        public async Task Refresh_ComTokenDesconhecido_DeveRetornarNull()
        {
            _repository.GetByRefreshTokenHashAsync(Arg.Any<string>()).Returns((Usuario?)null);

            var service = CriarServico();

            var resposta = await service.RefreshAsync(
                new RefreshTokenRequest { RefreshToken = "nao-existe" });

            Assert.Null(resposta);
            _tokenService.DidNotReceive().GerarToken(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task Refresh_ComTokenExpirado_DeveRetornarNull()
        {
            var usuario = new Usuario
            {
                Id = 1,
                RefreshTokenHash = HashDe("refresh-expirado"),
                RefreshTokenExpiraEm = DateTime.UtcNow.AddMinutes(-1)
            };
            _repository.GetByRefreshTokenHashAsync(HashDe("refresh-expirado")).Returns(usuario);

            var service = CriarServico();

            var resposta = await service.RefreshAsync(
                new RefreshTokenRequest { RefreshToken = "refresh-expirado" });

            Assert.Null(resposta);
            _tokenService.DidNotReceive().GerarToken(Arg.Any<Usuario>());
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task Logout_DeveRevogarRefreshToken()
        {
            var usuario = new Usuario
            {
                Id = 1,
                RefreshTokenHash = HashDe("refresh-token"),
                RefreshTokenExpiraEm = DateTime.UtcNow.AddDays(1)
            };
            _repository.GetByIdAsync(1).Returns(usuario);

            var service = CriarServico();

            await service.LogoutAsync(1);

            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                u.RefreshTokenHash == null && u.RefreshTokenExpiraEm == null));
        }
    }
}
