using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.UseCases.Veiculos.Validator;

namespace Frota360.Tests.UseCases.Veiculos
{
    /// <summary>
    /// RN09 — formato da placa. Os dois padrões brasileiros convivem na frota, então nenhum
    /// dos dois pode ser recusado; a caixa é indiferente porque o handler normaliza.
    /// </summary>
    public class VeiculoValidatorTests
    {
        private static CreateVeiculoRequest ComPlaca(string placa) => new()
        {
            NomeVeiculo = "Strada",
            MarcaVeiculo = "Fiat",
            Placa = placa,
            Quilometragem = 1000
        };

        [Theory]
        [InlineData("ABC1234")]  // antigo
        [InlineData("ABC1D23")]  // Mercosul
        [InlineData("abc1d23")]  // minúsculas — normalizadas pelo handler
        public void Placa_EmFormatoValido_DeveSerAceita(string placa)
        {
            var resultado = new CreateVeiculoValidator().Validate(ComPlaca(placa));

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("ABCD123")]   // quatro letras
        [InlineData("AB1234")]    // duas letras
        [InlineData("ABC-1234")]  // com separador
        [InlineData("ABC12D3")]   // letra fora da posição do padrão Mercosul
        [InlineData("")]
        public void Placa_EmFormatoInvalido_DeveSerRecusada(string placa)
        {
            var resultado = new CreateVeiculoValidator().Validate(ComPlaca(placa));

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CreateVeiculoRequest.Placa));
        }

        [Theory]
        [InlineData("ABC1234")]
        [InlineData("ABC1D23")]
        [InlineData("abc1d23")]
        public void Placa_NoUpdate_DeveSeguirAMesmaRegra(string placa)
        {
            var resultado = new UpdateVeiculoValidator().Validate(new UpdateVeiculoRequest
            {
                NomeVeiculo = "Strada",
                MarcaVeiculo = "Fiat",
                Placa = placa,
                Quilometragem = 1000
            });

            Assert.True(resultado.IsValid);
        }
    }
}
