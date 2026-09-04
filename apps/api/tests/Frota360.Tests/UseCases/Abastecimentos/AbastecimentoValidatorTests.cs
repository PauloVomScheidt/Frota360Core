using Frota360.Application.DTOs.Abastecimento.Request;
using Frota360.Application.UseCases.Abastecimentos.Validator;

namespace Frota360.Tests.UseCases.Abastecimentos
{
    public class AbastecimentoValidatorTests
    {
        private readonly CreateAbastecimentoValidator _validator = new();

        private static CreateAbastecimentoRequest Valido() => new()
        {
            VeiculoId = 5,
            MotoristaId = 7,
            TipoCombustivelId = 3,
            PostoId = 4,
            Litros = 48.5m,
            ValorLitro = 6.19m,
            Odometro = 152_340,
            NotaFiscal = "123456",
            DataAbastecimento = DateTime.Now.Date
        };

        [Fact]
        public void Validar_ComLancamentoValido_DeveAceitar()
        {
            Assert.True(_validator.Validate(Valido()).IsValid);
        }

        [Fact]
        public void Validar_SemMotorista_DeveAceitar()
        {
            // Se o motorista é obrigatório depende do papel de quem lança, e isso o
            // validator não sabe — a regra vive no handler.
            var request = Valido();
            request.MotoristaId = null;

            Assert.True(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_SemFrentista_DeveAceitar()
        {
            // Em autoatendimento não há frentista.
            var request = Valido();
            request.Frentista = null;

            Assert.True(_validator.Validate(request).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(2_001)]
        public void Validar_ComLitrosForaDaFaixa_DeveRecusar(decimal litros)
        {
            var request = Valido();
            request.Litros = litros;

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public void Validar_ComValorDoLitroForaDaFaixa_DeveRecusar(decimal valorLitro)
        {
            var request = Valido();
            request.ValorLitro = valorLitro;

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validar_ComOdometroInvalido_DeveRecusar(int odometro)
        {
            var request = Valido();
            request.Odometro = odometro;

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_SemCombustivel_DeveRecusar()
        {
            var request = Valido();
            request.TipoCombustivelId = 0;

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_SemPosto_DeveRecusar()
        {
            var request = Valido();
            request.PostoId = 0;

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_SemNotaFiscal_DeveRecusar()
        {
            var request = Valido();
            request.NotaFiscal = "";

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_ComDataNoFuturo_DeveRecusar()
        {
            // Não se agenda combustível: o abastecimento é sempre um fato já ocorrido.
            var request = Valido();
            request.DataAbastecimento = DateTime.Now.Date.AddDays(5);

            Assert.False(_validator.Validate(request).IsValid);
        }
    }
}
