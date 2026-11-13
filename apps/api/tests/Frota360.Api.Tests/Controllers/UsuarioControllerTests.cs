using Frota360.Api.Controllers;
using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class UsuarioControllerTests
    {
        private readonly IUsuarioService _usuarioService = Substitute.For<IUsuarioService>();

        private UsuarioController Controller(bool valido = true) => valido
            ? new(_usuarioService, Validacao.Aceita<AlterarRoleRequest>(), Validacao.Aceita<AtualizarPerfilRequest>())
            : new(_usuarioService, Validacao.Recusa<AlterarRoleRequest>(), Validacao.Recusa<AtualizarPerfilRequest>());

        [Fact]
        public async Task Listar_DeveDevolverOsUsuariosDaEmpresa()
        {
            IEnumerable<UsuarioResponse> usuarios = [new() { Id = 1 }];
            _usuarioService.ListarAsync().Returns(usuarios);

            Assert.Same(usuarios, Resposta.Ok<IEnumerable<UsuarioResponse>>(await Controller().Listar()));
        }

        [Fact]
        public async Task AlterarRole_DeveRepassarANovaRoleOu404()
        {
            _usuarioService.AlterarRoleAsync(3, Roles.Supervisor).Returns(new UsuarioResponse { Id = 3 });
            var request = new AlterarRoleRequest { Role = Roles.Supervisor };

            Resposta.Ok<UsuarioResponse>(await Controller().AlterarRole(3, request), "Role alterada com sucesso.");
            Resposta.NaoEncontrado(await Controller().AlterarRole(4, request), "Usuário 4 não encontrado.");
        }

        [Theory]
        [InlineData(true, "Usuário reativado com sucesso.")]
        [InlineData(false, "Usuário desativado com sucesso.")]
        public async Task DefinirAtivo_DeveDizerOQueAconteceu(bool ativo, string mensagem)
        {
            _usuarioService.DefinirAtivoAsync(3, ativo).Returns(new UsuarioResponse { Id = 3 });

            Resposta.Ok<UsuarioResponse>(await Controller().DefinirAtivo(3, new DefinirAtivoRequest { Ativo = ativo }), mensagem);
        }

        [Fact]
        public async Task DefinirAtivo_UsuarioInexistente_DeveResponder404()
        {
            Resposta.NaoEncontrado(await Controller().DefinirAtivo(4, new DefinirAtivoRequest()), "Usuário 4 não encontrado.");
        }

        [Fact]
        public async Task Perfil_DeveLerEAtualizarOProprioCadastroOu404()
        {
            var perfil = new PerfilResponse { Id = 42 };
            var request = new AtualizarPerfilRequest { Nome = "Maria" };
            _usuarioService.ObterPerfilAsync().Returns(perfil);
            _usuarioService.AtualizarPerfilAsync(request).Returns(perfil);

            Assert.Same(perfil, Resposta.Ok<PerfilResponse>(await Controller().ObterPerfil()));
            Resposta.Ok<PerfilResponse>(await Controller().AtualizarPerfil(request), "Perfil atualizado com sucesso.");

            _usuarioService.ObterPerfilAsync().Returns((PerfilResponse?)null);
            Resposta.NaoEncontrado(await Controller().ObterPerfil(), "Usuário não encontrado.");
            Resposta.NaoEncontrado(await Controller().AtualizarPerfil(new AtualizarPerfilRequest()), "Usuário não encontrado.");
        }

        [Fact]
        public async Task Formularios_ComDadosInvalidos_DevemResponder400SemChamarOServico()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.AlterarRole(3, new AlterarRoleRequest()));
            Resposta.Invalido(await controller.AtualizarPerfil(new AtualizarPerfilRequest()));
            Assert.Empty(_usuarioService.ReceivedCalls());
        }
    }
}
