using Frota360.Application.DTOs.Convite.Request;
using Frota360.Application.Validators.Convite;

namespace Frota360.Tests.Validators
{
    public class AceitarConviteValidatorTests
    {
        private readonly AceitarConviteValidator _validator = new();

        private static AceitarConviteRequest Valido() => new()
        {
            Token = "token-do-convite",
            Nome = "Maria Souza",
            Senha = "SenhaForte123"
        };

        [Fact]
        public void Validar_SemDadosPessoais_DeveAceitar()
        {
            Assert.True(_validator.Validate(Valido()).IsValid);
        }

        [Theory]
        [InlineData("52998224725")]
        [InlineData("11144477735")]
        [InlineData("12345678909")]
        public void Validar_ComCpfValido_DeveAceitar(string cpf)
        {
            var request = Valido();
            request.CPF = cpf;
            request.DataNascimento = DateTime.Today.AddYears(-30);

            Assert.True(_validator.Validate(request).IsValid);
        }

        [Theory]
        [InlineData("52998224715")]
        [InlineData("52998224726")]
        [InlineData("11111111111")]
        [InlineData("5299822472a")]
        [InlineData("529982247")]
        public void Validar_ComCpfInvalido_DeveRecusar(string cpf)
        {
            var request = Valido();
            request.CPF = cpf;

            var resultado = _validator.Validate(request);

            Assert.Contains(resultado.Errors, e => e.ErrorMessage == "CPF inválido.");
        }

        [Fact]
        public void Validar_ComTokenVazio_DeveRecusar()
        {
            var request = Valido();
            request.Token = "";

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Abc12")]
        [InlineData("semmaiuscula123")]
        [InlineData("SemNumero")]
        public void Validar_ComSenhaFraca_DeveRecusar(string senha)
        {
            var request = Valido();
            request.Senha = senha;

            Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == "Senha");
        }

        [Fact]
        public void Validar_ComNomeLongo_DeveRecusar()
        {
            var request = Valido();
            request.Nome = new string('a', 101);

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(-101)]
        public void Validar_ComNascimentoFuturoOuImplausivel_DeveRecusar(int anos)
        {
            var request = Valido();
            request.DataNascimento = DateTime.Today.AddYears(anos);

            Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == "DataNascimento");
        }
    }
}
