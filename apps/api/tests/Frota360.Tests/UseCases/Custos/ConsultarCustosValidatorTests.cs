using Frota360.Application.DTOs.Custo.Request;
using Frota360.Application.UseCases.Custos.Validator;

namespace Frota360.Tests.UseCases.Custos
{
    public class ConsultarCustosValidatorTests
    {
        private readonly ConsultarCustosValidator _validator = new();

        [Fact]
        public void Validar_ComFiltroPadrao_DeveAceitar()
        {
            var resultado = _validator.Validate(new ConsultarCustosRequest());

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Validar_ComTamanhoDePaginaAcimaDoTeto_DeveRecusar()
        {
            // Sem o teto, um tamanhoPagina absurdo materializa o histórico inteiro da empresa.
            var resultado = _validator.Validate(new ConsultarCustosRequest { TamanhoPagina = 999_999 });

            Assert.False(resultado.IsValid);
        }

        [Fact]
        public void Validar_ComPaginaZero_DeveRecusar()
        {
            var resultado = _validator.Validate(new ConsultarCustosRequest { Pagina = 0 });

            Assert.False(resultado.IsValid);
        }

        [Fact]
        public void Validar_ComOrigemForaDoVocabulario_DeveRecusar()
        {
            var resultado = _validator.Validate(new ConsultarCustosRequest { Origem = "Pedagio" });

            Assert.False(resultado.IsValid);
        }

        [Theory]
        [InlineData("Abastecimento")]
        [InlineData("Manutencao")]
        public void Validar_ComOrigemConhecida_DeveAceitar(string origem)
        {
            var resultado = _validator.Validate(new ConsultarCustosRequest { Origem = origem });

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Validar_ComPeriodoInvertido_DeveRecusar()
        {
            var resultado = _validator.Validate(new ConsultarCustosRequest
            {
                De = new DateTime(2026, 8, 31),
                Ate = new DateTime(2026, 8, 1)
            });

            Assert.False(resultado.IsValid);
        }

        [Fact]
        public void Validar_ResumoComPeriodoInvertido_DeveRecusar()
        {
            var resultado = new ResumoCustosValidator().Validate(new ResumoCustosRequest
            {
                De = new DateTime(2026, 8, 31),
                Ate = new DateTime(2026, 8, 1)
            });

            Assert.False(resultado.IsValid);
        }
    }
}
