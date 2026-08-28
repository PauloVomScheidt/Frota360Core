using Frota360.Application.Common;
using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.Services;
using Frota360.Domain.Common;
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
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();

        public UsuarioServiceTests()
        {
            _currentUser.EmpresaId.Returns(1);
        }

        private UsuarioService CriarServico() =>
            new(_repository, _currentUser, _auditoria, NullLogger<UsuarioService>.Instance);

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

        /// <summary>
        /// Mudança de permissão é o evento mais consequente da trilha — o que amplia ou reduz
        /// o alcance de alguém no sistema inteiro. O diff precisa guardar o papel anterior.
        /// </summary>
        [Fact]
        public async Task AlterarRole_DeveRegistrarAuditoriaComOPapelAnterior()
        {
            var operador = NovoAdmin(2);
            operador.Role = "Operador";
            _repository.GetByIdAsync(2).Returns(operador);
            _repository.UpdateAsync(Arg.Any<Usuario>()).Returns(ci => ci.Arg<Usuario>());

            var service = CriarServico();

            await service.AlterarRoleAsync(2, "Admin");

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Usuario,
                AcoesAuditoria.AlterouPermissao,
                2,
                Arg.Any<string>(),
                Arg.Is<IEnumerable<AlteracaoCampo>>(a =>
                    a.Single().Campo == "Permissão" && a.Single().De == "Operador" && a.Single().Para == "Admin"));
        }

        [Fact]
        public async Task DefinirAtivo_Desativar_DeveRegistrarAuditoriaComAAcaoDesativou()
        {
            var operador = NovoAdmin(2);
            operador.Role = "Operador";
            _repository.GetByIdAsync(2).Returns(operador);
            _repository.UpdateAsync(Arg.Any<Usuario>()).Returns(ci => ci.Arg<Usuario>());

            var service = CriarServico();

            await service.DefinirAtivoAsync(2, false);

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Usuario, AcoesAuditoria.Desativou, 2,
                Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>>());
        }

        [Fact]
        public async Task AlterarRole_ParaAMesmaRole_NaoDeveRegistrarAuditoria()
        {
            var operador = NovoAdmin(2);
            operador.Role = "Operador";
            _repository.GetByIdAsync(2).Returns(operador);

            var service = CriarServico();

            await service.AlterarRoleAsync(2, "Operador");

            // Nada mudou de fato — não vira linha na trilha.
            await _auditoria.DidNotReceive().RegistrarAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>>());
        }

        // ----- Perfil (direito de correção da LGPD) -----

        [Fact]
        public async Task AtualizarPerfil_DeveEditarODonoDoTokenIgnorandoQualquerOutroId()
        {
            _currentUser.UsuarioId.Returns(7);
            var proprio = NovoAdmin(7);
            _repository.GetByIdAsync(7).Returns(proprio);
            _repository.UpdateAsync(Arg.Any<Usuario>()).Returns(ci => ci.Arg<Usuario>());

            var service = CriarServico();

            var resposta = await service.AtualizarPerfilAsync(new AtualizarPerfilRequest
            {
                Nome = "Admin Corrigido",
                CPF = "52998224725",
                DataNascimento = new DateTime(1990, 5, 20)
            });

            Assert.NotNull(resposta);
            // O request não carrega id: o alvo só pode ter vindo do claim `sub`.
            await _repository.Received(1).GetByIdAsync(7);
            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                u.Id == 7 && u.Nome == "Admin Corrigido" && u.CPF == "52998224725"));
        }

        [Fact]
        public async Task AtualizarPerfil_CpfEmBranco_DeveGravarNulo()
        {
            _currentUser.UsuarioId.Returns(7);
            var proprio = NovoAdmin(7);
            proprio.CPF = "52998224725";
            _repository.GetByIdAsync(7).Returns(proprio);
            _repository.UpdateAsync(Arg.Any<Usuario>()).Returns(ci => ci.Arg<Usuario>());

            var service = CriarServico();

            await service.AtualizarPerfilAsync(new AtualizarPerfilRequest { Nome = "Admin", CPF = "   " });

            // String vazia colidiria com todas as outras no índice único filtrado.
            await _repository.Received(1).UpdateAsync(Arg.Is<Usuario>(u => u.CPF == null));
        }

        [Fact]
        public async Task AtualizarPerfil_CpfDeOutroUsuarioDaMesmaEmpresa_DeveLancar()
        {
            _currentUser.UsuarioId.Returns(7);
            _repository.GetByIdAsync(7).Returns(NovoAdmin(7));
            _repository.ExisteCpfNaEmpresaAsync(1, "52998224725", 7).Returns(true);

            var service = CriarServico();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AtualizarPerfilAsync(new AtualizarPerfilRequest { Nome = "Admin", CPF = "52998224725" }));

            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task AtualizarPerfil_DeveRegistrarAuditoriaComODiffDosTresCampos()
        {
            _currentUser.UsuarioId.Returns(7);
            var proprio = NovoAdmin(7);
            _repository.GetByIdAsync(7).Returns(proprio);
            _repository.UpdateAsync(Arg.Any<Usuario>()).Returns(ci => ci.Arg<Usuario>());

            var service = CriarServico();

            await service.AtualizarPerfilAsync(new AtualizarPerfilRequest
            {
                Nome = "Admin Corrigido",
                CPF = "52998224725",
                DataNascimento = new DateTime(1990, 5, 20)
            });

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Usuario,
                AcoesAuditoria.Atualizou,
                7,
                Arg.Any<string>(),
                // Só os três campos declarados: nenhum hash de senha ou token pode entrar aqui.
                Arg.Is<IEnumerable<AlteracaoCampo>>(a =>
                    a.Count() == 3
                    && a.Any(c => c.Campo == "Nome")
                    && a.Any(c => c.Campo == "CPF")
                    && a.Any(c => c.Campo == "Data de nascimento")));
        }
    }
}
