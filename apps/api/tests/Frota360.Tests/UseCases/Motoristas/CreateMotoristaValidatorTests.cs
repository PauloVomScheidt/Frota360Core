using Frota360.Application.DTOs.Motorista.Request;
using Frota360.Application.UseCases.Motoristas.Validator;

namespace Frota360.Tests.UseCases.Motoristas
{
    public class CreateMotoristaValidatorTests
    {
        private readonly CreateMotoristaValidator _validator = new();

        private static CreateMotoristaRequest RequestValido() => new()
        {
            Nome = "João da Silva",
            Email = "joao@email.com",
            CPF = "39053344705", // CPF válido
            DataNascimento = new DateTime(1990, 1, 1)
        };

        [Fact]
        public void RequestValido_DevePassar()
        {
            var resultado = _validator.Validate(RequestValido());
            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Nome_Vazio_DeveFalhar()
        {
            var request = RequestValido();
            request.Nome = "";

            var resultado = _validator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.Nome));
        }

        [Theory]
        [InlineData("email-invalido")]
        [InlineData("sem@")]
        [InlineData("")]
        public void Email_Invalido_DeveFalhar(string email)
        {
            var request = RequestValido();
            request.Email = email;

            var resultado = _validator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.Email));
        }

        [Theory]
        [InlineData("123")]            // tamanho incorreto
        [InlineData("abcdefghijk")]    // não numérico
        [InlineData("11111111111")]    // dígitos repetidos
        [InlineData("39053344704")]    // dígito verificador inválido
        public void CPF_Invalido_DeveFalhar(string cpf)
        {
            var request = RequestValido();
            request.CPF = cpf;

            var resultado = _validator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.CPF));
        }

        [Fact]
        public void MenorDe18Anos_DeveFalhar()
        {
            var request = RequestValido();
            request.DataNascimento = DateTime.Today.AddYears(-17);

            var resultado = _validator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.DataNascimento));
        }

        [Fact]
        public void DataNascimentoFutura_DeveFalhar()
        {
            var request = RequestValido();
            request.DataNascimento = DateTime.Today.AddYears(1);

            var resultado = _validator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.DataNascimento));
        }
    }
}
