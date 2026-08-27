using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.UseCases.Rotas.Validator;

namespace Frota360.Tests.UseCases.Rotas
{
    public class RotaValidatorTests
    {
        private readonly CreateRotaValidator _createValidator = new();
        private readonly EncerrarRotaValidator _encerrarValidator = new();

        private static CreateRotaRequest CreateValido() => new()
        {
            Origem = "Joinville",
            Destino = "Blumenau",
            CodigoMotorista = 2,
            CodigoVeiculo = 3,
            DataInicio = new DateTime(2025, 6, 1),
            KmInicial = 50_000
        };

        [Fact]
        public void Create_RequestValido_DevePassar()
        {
            var resultado = _createValidator.Validate(CreateValido());
            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-50_000)]
        [InlineData(2_000_001)]
        public void Create_KmInicialInvalido_DeveFalhar(int kmInicial)
        {
            var request = CreateValido();
            request.KmInicial = kmInicial;

            var resultado = _createValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.KmInicial));
        }

        [Fact]
        public void Create_KmInicialZero_DevePassar()
        {
            var request = CreateValido();
            request.KmInicial = 0;

            var resultado = _createValidator.Validate(request);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Encerrar_RequestValido_DevePassar()
        {
            var resultado = _encerrarValidator.Validate(
                new EncerrarRotaRequest { KmFinal = 50_430, DataFim = DateTime.UtcNow });

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Encerrar_SemDataFim_DevePassar()
        {
            var resultado = _encerrarValidator.Validate(new EncerrarRotaRequest { KmFinal = 50_430 });

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2_000_001)]
        public void Encerrar_KmFinalInvalido_DeveFalhar(int kmFinal)
        {
            var request = new EncerrarRotaRequest { KmFinal = kmFinal };

            var resultado = _encerrarValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.KmFinal));
        }

        [Fact]
        public void Encerrar_DataFimNoFuturo_DeveFalhar()
        {
            var request = new EncerrarRotaRequest
            {
                KmFinal = 50_430,
                DataFim = DateTime.UtcNow.AddDays(5)
            };

            var resultado = _encerrarValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.DataFim));
        }
    }
}
