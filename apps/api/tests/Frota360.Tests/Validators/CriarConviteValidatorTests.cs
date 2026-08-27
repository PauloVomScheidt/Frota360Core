using Frota360.Application.DTOs.Convite.Request;
using Frota360.Application.Validators.Convite;
using Frota360.Domain.Common;

namespace Frota360.Tests.Validators
{
    /// <summary>
    /// Motorista é uma role como as outras: o convite não pede nada além de e-mail e
    /// perfil. O que o validador guarda aqui é só o conjunto de roles aceitas.
    /// </summary>
    public class CriarConviteValidatorTests
    {
        private readonly CriarConviteValidator _validator = new();

        private static CriarConviteRequest RequestValido(string role = Roles.Operador) => new()
        {
            Email = "convidado@email.com",
            Role = role
        };

        [Theory]
        [InlineData(Roles.Admin)]
        [InlineData(Roles.Supervisor)]
        [InlineData(Roles.Operador)]
        [InlineData(Roles.Motorista)]
        public void TodasAsRolesDoSistema_DevemPassar(string role)
        {
            Assert.True(_validator.Validate(RequestValido(role)).IsValid);
        }

        [Fact]
        public void RoleInexistente_DeveFalhar()
        {
            var resultado = _validator.Validate(RequestValido("Motoboy"));

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CriarConviteRequest.Role));
        }

        [Fact]
        public void EmailInvalido_DeveFalhar()
        {
            var request = RequestValido();
            request.Email = "nao-e-email";

            var resultado = _validator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CriarConviteRequest.Email));
        }
    }
}
