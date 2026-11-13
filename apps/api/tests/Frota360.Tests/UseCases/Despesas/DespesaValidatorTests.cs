using Frota360.Application.DTOs.Despesa.Request;
using Frota360.Application.UseCases.Despesas.Validator;

namespace Frota360.Tests.UseCases.Despesas
{
    public class DespesaValidatorTests
    {
        private readonly CreateDespesaValidator _validator = new();

        private static CreateDespesaRequest Valido() => new()
        {
            VeiculoId = 5,
            TipoDespesaId = 3,
            Valor = 100m,
            DataDespesa = DateTime.Now.Date
        };

        [Fact]
        public void Validar_ComLancamentoValido_DeveAceitar()
        {
            Assert.True(_validator.Validate(Valido()).IsValid);
        }

        [Fact]
        public void Validar_SemMotorista_DeveAceitar()
        {
            // IPVA e seguro não são de ninguém — o campo é opcional de propósito.
            var request = Valido();
            request.MotoristaId = null;

            Assert.True(_validator.Validate(request).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1_000_001)]
        public void Validar_ComValorForaDaFaixa_DeveRecusar(decimal valor)
        {
            var request = Valido();
            request.Valor = valor;

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_SemVeiculo_DeveRecusar()
        {
            var request = Valido();
            request.VeiculoId = 0;

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_SemTipo_DeveRecusar()
        {
            var request = Valido();
            request.TipoDespesaId = 0;

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_ComDataFutura_DeveRecusar()
        {
            var request = Valido();
            request.DataDespesa = DateTime.Now.Date.AddDays(5);

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void Validar_ComObservacaoAcimaDoLimite_DeveRecusar()
        {
            var request = Valido();
            request.Observacao = new string('x', 501);

            Assert.False(_validator.Validate(request).IsValid);
        }

        [Fact]
        public void ValidarUpdate_ComPayloadValido_DeveAceitar()
        {
            var resultado = new UpdateDespesaValidator().Validate(new UpdateDespesaRequest
            {
                VeiculoId = 5,
                TipoDespesaId = 3,
                Valor = 250m,
                DataDespesa = DateTime.Now.Date
            });

            Assert.True(resultado.IsValid);
        }
    }
}
