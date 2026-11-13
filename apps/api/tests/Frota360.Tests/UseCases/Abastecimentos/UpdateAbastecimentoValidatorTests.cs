using Frota360.Application.DTOs.Abastecimento.Request;
using Frota360.Application.UseCases.Abastecimentos.Validator;

namespace Frota360.Tests.UseCases.Abastecimentos
{
    public class UpdateAbastecimentoValidatorTests
    {
        private readonly UpdateAbastecimentoValidator _validator = new();

        private static UpdateAbastecimentoRequest Valido() => new()
        {
            TipoCombustivelId = 1,
            PostoId = 2,
            Litros = 40m,
            ValorLitro = 6.19m,
            Odometro = 120_000,
            NotaFiscal = "NF-123",
            DataAbastecimento = DateTime.Now.Date
        };

        [Fact]
        public void Validar_ComCorrecaoValida_DeveAceitar()
        {
            Assert.True(_validator.Validate(Valido()).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2_001)]
        public void Validar_ComLitrosForaDaFaixa_DeveRecusar(decimal litros)
        {
            var request = Valido();
            request.Litros = litros;

            Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == "Litros");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Validar_ComValorDoLitroForaDaFaixa_DeveRecusar(decimal valorLitro)
        {
            var request = Valido();
            request.ValorLitro = valorLitro;

            Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == "ValorLitro");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10_000_001)]
        public void Validar_ComOdometroForaDaFaixa_DeveRecusar(int odometro)
        {
            var request = Valido();
            request.Odometro = odometro;

            Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == "Odometro");
        }

        [Fact]
        public void Validar_SemCatalogos_DeveRecusarCombustivelEPosto()
        {
            var request = Valido();
            request.TipoCombustivelId = 0;
            request.PostoId = 0;

            var erros = _validator.Validate(request).Errors.Select(e => e.PropertyName).ToList();

            Assert.Contains("TipoCombustivelId", erros);
            Assert.Contains("PostoId", erros);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1234567890123456789012345678901")]
        public void Validar_ComNotaFiscalVaziaOuLonga_DeveRecusar(string notaFiscal)
        {
            var request = Valido();
            request.NotaFiscal = notaFiscal;

            Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == "NotaFiscal");
        }

        [Fact]
        public void Validar_ComDataFutura_DeveRecusar()
        {
            var request = Valido();
            request.DataAbastecimento = DateTime.Now.Date.AddDays(2);

            Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == "DataAbastecimento");
        }

        [Fact]
        public void Validar_ComTextosLivresLongos_DeveRecusarFrentistaEObservacao()
        {
            var request = Valido();
            request.Frentista = new string('f', 101);
            request.Observacao = new string('o', 501);

            var erros = _validator.Validate(request).Errors.Select(e => e.PropertyName).ToList();

            Assert.Contains("Frentista", erros);
            Assert.Contains("Observacao", erros);
        }
    }
}
