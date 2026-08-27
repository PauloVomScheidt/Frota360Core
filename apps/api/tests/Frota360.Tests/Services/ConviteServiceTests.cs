using Frota360.Application.Common;
using Frota360.Application.DTOs.Convite.Request;
using Frota360.Application.Interfaces;
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
    public class ConviteServiceTests
    {
        private readonly IConviteRepository _conviteRepository = Substitute.For<IConviteRepository>();
        private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
        private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
        private readonly IEmailService _emailService = Substitute.For<IEmailService>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

        public ConviteServiceTests()
        {
            _currentUser.EmpresaId.Returns(1);
            _currentUser.UsuarioId.Returns(10);
            _conviteRepository.GetPendentesByEmailAsync(Arg.Any<string>(), Arg.Any<int>())
                .Returns([]);
            _conviteRepository.AddAsync(Arg.Any<Convite>()).Returns(ci =>
            {
                var c = ci.Arg<Convite>();
                c.Id = 5;
                return c;
            });
            _usuarioRepository.AddAsync(Arg.Any<Usuario>()).Returns(ci =>
            {
                var u = ci.Arg<Usuario>();
                u.Id = 99;
                return u;
            });
        }

        private ConviteService CriarServico() =>
            new(_conviteRepository, _usuarioRepository, _tokenService, _emailService, _currentUser,
                new FrontendSettings("http://localhost:5173"), NullLogger<ConviteService>.Instance);

        private static string HashDe(string token) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        [Fact]
        public async Task Criar_DevePersistirApenasHash_EEnviarEmailComLink()
        {
            _usuarioRepository.ExisteEmailAsync("nova@email.com").Returns(false);
            _tokenService.GerarRefreshToken().Returns("token-convite");

            var service = CriarServico();

            var resposta = await service.CriarAsync(
                new CriarConviteRequest { Email = "nova@email.com", Role = "Operador" });

            Assert.Contains("token-convite", Uri.UnescapeDataString(resposta.LinkConvite));
            await _conviteRepository.Received(1).AddAsync(Arg.Is<Convite>(c =>
                c.EmpresaId == 1 &&
                c.CriadoPorUsuarioId == 10 &&
                c.TokenHash == HashDe("token-convite") &&
                c.ExpiraEm > DateTime.UtcNow));
            await _emailService.Received(1).EnviarAsync("nova@email.com",
                Arg.Any<string>(), Arg.Is<string>(corpo => corpo.Contains("token-convite")));
        }

        [Fact]
        public async Task Criar_EmailJaCadastrado_DeveLancar()
        {
            _usuarioRepository.ExisteEmailAsync("existe@email.com").Returns(true);

            var service = CriarServico();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CriarAsync(new CriarConviteRequest { Email = "existe@email.com", Role = "Admin" }));

            await _conviteRepository.DidNotReceive().AddAsync(Arg.Any<Convite>());
        }

        [Fact]
        public async Task Criar_ComConvitePendenteAnterior_DeveInvalidarOAnterior()
        {
            var pendente = new Convite { Id = 3, Email = "nova@email.com", EmpresaId = 1 };
            _conviteRepository.GetPendentesByEmailAsync("nova@email.com", 1).Returns([pendente]);
            _tokenService.GerarRefreshToken().Returns("token-novo");

            var service = CriarServico();

            await service.CriarAsync(new CriarConviteRequest { Email = "nova@email.com", Role = "Operador" });

            await _conviteRepository.Received(1).DeleteAsync(pendente);
        }

        [Fact]
        public async Task Aceitar_TokenValido_DeveCriarUsuarioNaEmpresaERoleDoConvite_EMarcarUtilizado()
        {
            var convite = new Convite
            {
                Id = 5,
                EmpresaId = 7,
                Email = "convidada@email.com",
                Role = "Supervisor",
                TokenHash = HashDe("token-convite"),
                ExpiraEm = DateTime.UtcNow.AddDays(1)
            };
            _conviteRepository.GetByTokenHashAsync(HashDe("token-convite")).Returns(convite);
            _usuarioRepository.ExisteEmailAsync("convidada@email.com").Returns(false);
            _tokenService.GerarRefreshToken().Returns("refresh-novo");
            _tokenService.GerarToken(Arg.Any<Usuario>()).Returns("jwt-novo");

            var service = CriarServico();

            var resposta = await service.AceitarAsync(
                new AceitarConviteRequest { Token = "token-convite", Nome = "Ana", Senha = "SenhaForte1" });

            Assert.NotNull(resposta);
            Assert.Equal("jwt-novo", resposta!.Token);
            Assert.Equal("Supervisor", resposta.Role);
            await _usuarioRepository.Received(1).AddAsync(Arg.Is<Usuario>(u =>
                u.EmpresaId == 7 &&
                u.Role == "Supervisor" &&
                u.Email == "convidada@email.com" &&
                u.Ativo &&
                u.SenhaHash != "SenhaForte1" &&
                BCrypt.Net.BCrypt.Verify("SenhaForte1", u.SenhaHash)));
            await _conviteRepository.Received(1).UpdateAsync(Arg.Is<Convite>(c => c.UtilizadoEm != null));
        }

        [Fact]
        public async Task Aceitar_TokenDesconhecido_DeveRetornarNull()
        {
            _conviteRepository.GetByTokenHashAsync(Arg.Any<string>()).Returns((Convite?)null);

            var service = CriarServico();

            var resposta = await service.AceitarAsync(
                new AceitarConviteRequest { Token = "nao-existe", Nome = "X", Senha = "SenhaForte1" });

            Assert.Null(resposta);
            await _usuarioRepository.DidNotReceive().AddAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task Aceitar_TokenExpirado_DeveRetornarNull()
        {
            var convite = new Convite
            {
                TokenHash = HashDe("expirado"),
                ExpiraEm = DateTime.UtcNow.AddMinutes(-1)
            };
            _conviteRepository.GetByTokenHashAsync(HashDe("expirado")).Returns(convite);

            var service = CriarServico();

            var resposta = await service.AceitarAsync(
                new AceitarConviteRequest { Token = "expirado", Nome = "X", Senha = "SenhaForte1" });

            Assert.Null(resposta);
        }

        [Fact]
        public async Task Aceitar_TokenJaUtilizado_DeveRetornarNull()
        {
            var convite = new Convite
            {
                TokenHash = HashDe("usado"),
                ExpiraEm = DateTime.UtcNow.AddDays(1),
                UtilizadoEm = DateTime.UtcNow.AddHours(-1)
            };
            _conviteRepository.GetByTokenHashAsync(HashDe("usado")).Returns(convite);

            var service = CriarServico();

            var resposta = await service.AceitarAsync(
                new AceitarConviteRequest { Token = "usado", Nome = "X", Senha = "SenhaForte1" });

            Assert.Null(resposta);
        }

        [Fact]
        public async Task Cancelar_ConviteJaUtilizado_DeveLancar()
        {
            var convite = new Convite { Id = 4, EmpresaId = 1, UtilizadoEm = DateTime.UtcNow };
            _conviteRepository.GetByIdAsync(4, 1).Returns(convite);

            var service = CriarServico();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelarAsync(4));
            await _conviteRepository.DidNotReceive().DeleteAsync(Arg.Any<Convite>());
        }
    }
}
