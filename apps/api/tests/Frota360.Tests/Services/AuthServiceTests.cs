using Frota360.Application.Common;
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
        private readonly IEmailService _emailService = Substitute.For<IEmailService>();

        private AuthService CriarServico() =>
            new(_repository, _tokenService, _emailService,
                new FrontendSettings("http://localhost:5173"), NullLogger<AuthService>.Instance);

        private static string HashDe(string refreshToken) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

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
                u.RefreshTokenExpiraEm > DateTime.Now));
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
        public async Task Login_UsuarioDesativado_DeveRetornarNull()
        {
            var usuario = new Usuario
            {
                Id = 1,
                Email = "ana@email.com",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("SenhaForte123"),
                Ativo = false
            };
            _repository.GetByEmailAsync("ana@email.com").Returns(usuario);

            var service = CriarServico();

            var resposta = await service.LoginAsync(
                new LoginRequest { Email = "ana@email.com", Senha = "SenhaForte123" });

            Assert.Null(resposta);
            _tokenService.DidNotReceive().GerarToken(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task Refresh_UsuarioDesativado_DeveRetornarNull()
        {
            var usuario = new Usuario
            {
                Id = 1,
                RefreshTokenHash = HashDe("refresh-token"),
                RefreshTokenExpiraEm = DateTime.Now.AddDays(1),
                Ativo = false
            };
            _repository.GetByRefreshTokenHashAsync(HashDe("refresh-token")).Returns(usuario);

            var service = CriarServico();

            var resposta = await service.RefreshAsync(
                new RefreshTokenRequest { RefreshToken = "refresh-token" });

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
                RefreshTokenExpiraEm = DateTime.Now.AddDays(1)
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
                RefreshTokenExpiraEm = DateTime.Now.AddMinutes(-1)
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
        public async Task EsqueciSenha_UsuarioExistente_DevePersistirHashEEnviarEmailComLink()
        {
            var usuario = new Usuario { Id = 1, Email = "ana@email.com", Ativo = true };
            _repository.GetByEmailAsync("ana@email.com").Returns(usuario);
            _tokenService.GerarRefreshToken().Returns("token-reset");

            var service = CriarServico();

            await service.EsqueciSenhaAsync(new EsqueciSenhaRequest { Email = "ana@email.com" });

            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                u.ResetSenhaTokenHash == HashDe("token-reset") &&
                u.ResetSenhaExpiraEm > DateTime.Now));
            await _emailService.Received(1).EnviarAsync("ana@email.com",
                Arg.Any<string>(), Arg.Is<string>(corpo => corpo.Contains("token-reset")));
        }

        [Fact]
        public async Task EsqueciSenha_EmailInexistente_NaoDeveEnviarNemLancar()
        {
            _repository.GetByEmailAsync(Arg.Any<string>()).Returns((Usuario?)null);

            var service = CriarServico();

            await service.EsqueciSenhaAsync(new EsqueciSenhaRequest { Email = "naoexiste@email.com" });

            await _emailService.DidNotReceive().EnviarAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task EsqueciSenha_UsuarioDesativado_NaoDeveEnviar()
        {
            var usuario = new Usuario { Id = 1, Email = "ana@email.com", Ativo = false };
            _repository.GetByEmailAsync("ana@email.com").Returns(usuario);

            var service = CriarServico();

            await service.EsqueciSenhaAsync(new EsqueciSenhaRequest { Email = "ana@email.com" });

            await _emailService.DidNotReceive().EnviarAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task RedefinirSenha_TokenValido_DeveTrocarSenhaLimparResetERevogarSessao()
        {
            var usuario = new Usuario
            {
                Id = 1,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("SenhaAntiga1"),
                ResetSenhaTokenHash = HashDe("token-reset"),
                ResetSenhaExpiraEm = DateTime.Now.AddMinutes(10),
                RefreshTokenHash = "sessao-ativa",
                RefreshTokenExpiraEm = DateTime.Now.AddDays(1)
            };
            _repository.GetByResetSenhaTokenHashAsync(HashDe("token-reset")).Returns(usuario);

            var service = CriarServico();

            var resultado = await service.RedefinirSenhaAsync(
                new RedefinirSenhaRequest { Token = "token-reset", NovaSenha = "SenhaNova1" });

            Assert.True(resultado);
            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                BCrypt.Net.BCrypt.Verify("SenhaNova1", u.SenhaHash) &&
                u.ResetSenhaTokenHash == null &&
                u.ResetSenhaExpiraEm == null &&
                u.RefreshTokenHash == null &&
                u.RefreshTokenExpiraEm == null));
        }

        [Fact]
        public async Task RedefinirSenha_TokenExpirado_DeveRetornarFalse()
        {
            var usuario = new Usuario
            {
                Id = 1,
                ResetSenhaTokenHash = HashDe("token-expirado"),
                ResetSenhaExpiraEm = DateTime.Now.AddMinutes(-1)
            };
            _repository.GetByResetSenhaTokenHashAsync(HashDe("token-expirado")).Returns(usuario);

            var service = CriarServico();

            var resultado = await service.RedefinirSenhaAsync(
                new RedefinirSenhaRequest { Token = "token-expirado", NovaSenha = "SenhaNova1" });

            Assert.False(resultado);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task RedefinirSenha_TokenDesconhecido_DeveRetornarFalse()
        {
            _repository.GetByResetSenhaTokenHashAsync(Arg.Any<string>()).Returns((Usuario?)null);

            var service = CriarServico();

            var resultado = await service.RedefinirSenhaAsync(
                new RedefinirSenhaRequest { Token = "nao-existe", NovaSenha = "SenhaNova1" });

            Assert.False(resultado);
        }

        [Fact]
        public async Task Logout_DeveRevogarRefreshToken()
        {
            var usuario = new Usuario
            {
                Id = 1,
                RefreshTokenHash = HashDe("refresh-token"),
                RefreshTokenExpiraEm = DateTime.Now.AddDays(1)
            };
            _repository.GetByIdAsync(1).Returns(usuario);

            var service = CriarServico();

            await service.LogoutAsync(1);

            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                u.RefreshTokenHash == null && u.RefreshTokenExpiraEm == null));
        }
    }
}
