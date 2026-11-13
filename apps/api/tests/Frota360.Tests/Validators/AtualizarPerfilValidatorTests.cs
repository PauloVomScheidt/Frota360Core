using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.Validators.Usuario;

namespace Frota360.Tests.Validators
{
    public class AtualizarPerfilValidatorTests
    {
        private readonly AtualizarPerfilValidator _validator = new();

        [Fact]
        public void Validar_SoComNome_DeveAceitar()
        {
            Assert.True(_validator.Validate(new AtualizarPerfilRequest { Nome = "Maria Souza" }).IsValid);
        }

        [Fact]
        public void Validar_ComCpfENascimentoValidos_DeveAceitar()
        {
            var request = new AtualizarPerfilRequest
            {
                Nome = "Maria Souza",
                CPF = "52998224725",
                DataNascimento = DateTime.Today.AddYears(-40)
            };

            Assert.True(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_CpfEmBranco_DeveIgnorarAsRegrasDeCpf()
        {
            Assert.True(_validator.Validate(new AtualizarPerfilRequest { Nome = "Maria", CPF = "   " }).IsValid);
        }

        [Theory]
        [InlineData("52998224724")]
        [InlineData("00000000000")]
        [InlineData("529.982.247-25")]
        public void Validar_ComCpfInvalido_DeveRecusar(string cpf)
        {
            var resultado = _validator.Validate(new AtualizarPerfilRequest { Nome = "Maria", CPF = cpf });

            Assert.Contains(resultado.Errors, e => e.PropertyName == "CPF");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validar_SemNome_DeveRecusar(string? nome)
        {
            Assert.False(_validator.Validate(new AtualizarPerfilRequest { Nome = nome! }).IsValid);
        }

        [Fact]
        public void Validar_ComNascimentoFuturo_DeveRecusar()
        {
            var request = new AtualizarPerfilRequest { Nome = "Maria", DataNascimento = DateTime.Today.AddDays(1) };

            Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == "DataNascimento");
        }
    }
}
