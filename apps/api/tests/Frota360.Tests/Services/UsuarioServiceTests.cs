using Frota360.Application.Interfaces;
using Frota360.Application.Services;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.Services
{
    public class UsuarioServiceTests
    {
        private readonly IUsuarioRepository _repository = Substitute.For<IUsuarioRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

        public UsuarioServiceTests()
        {
            _currentUser.EmpresaId.Returns(1);
        }

        private UsuarioService CriarServico() =>
            new(_repository, _currentUser, NullLogger<UsuarioService>.Instance);

        private static Usuario NovoAdmin(int id = 1) => new()
        {
            Id = id,
            EmpresaId = 1,
            Nome = "Admin",
            Email = "admin@email.com",
            Role = "Admin",
            Ativo = true,
            RefreshTokenHash = "hash",
            RefreshTokenExpiraEm = DateTime.UtcNow.AddDays(1)
        };

        [Fact]
        public async Task AlterarRole_UltimoAdminAtivo_DeveLancar()
        {
            _repository.GetByIdAsync(1).Returns(NovoAdmin());
            _repository.ContarAdminsAtivosAsync(1).Returns(1);

            var service = CriarServico();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AlterarRoleAsync(1, "Operador"));

            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task AlterarRole_ComOutroAdminAtivo_DeveAlterarERevogarSessao()
        {
            _repository.GetByIdAsync(1).Returns(NovoAdmin());
            _repository.ContarAdminsAtivosAsync(1).Returns(2);
            _repository.UpdateAsync(Arg.Any<Usuario>()).Returns(ci => ci.Arg<Usuario>());

            var service = CriarServico();

            var resposta = await service.AlterarRoleAsync(1, "Supervisor");

            Assert.NotNull(resposta);
            Assert.Equal("Supervisor", resposta!.Role);
            // Sessão revogada para forçar novo login com a claim de role atualizada
            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                u.Role == "Supervisor" && u.RefreshTokenHash == null && u.RefreshTokenExpiraEm == null));
        }


        [Fact]
        public async Task AlterarRole_UsuarioDeOutraEmpresa_DeveRetornarNull()
        {
            var deOutraEmpresa = NovoAdmin();
            deOutraEmpresa.EmpresaId = 2;
            _repository.GetByIdAsync(1).Returns(deOutraEmpresa);

            var service = CriarServico();

            var resposta = await service.AlterarRoleAsync(1, "Operador");

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task DefinirAtivo_DesativarUltimoAdminAtivo_DeveLancar()
        {
            _repository.GetByIdAsync(1).Returns(NovoAdmin());
            _repository.ContarAdminsAtivosAsync(1).Returns(1);

            var service = CriarServico();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DefinirAtivoAsync(1, false));

            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task DefinirAtivo_Desativar_DeveRevogarSessao()
        {
            var operador = NovoAdmin(2);
            operador.Role = "Operador";
            _repository.GetByIdAsync(2).Returns(operador);
            _repository.UpdateAsync(Arg.Any<Usuario>()).Returns(ci => ci.Arg<Usuario>());

            var service = CriarServico();

            var resposta = await service.DefinirAtivoAsync(2, false);

            Assert.NotNull(resposta);
            Assert.False(resposta!.Ativo);
            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                !u.Ativo && u.RefreshTokenHash == null && u.RefreshTokenExpiraEm == null));
        }
    }
}
