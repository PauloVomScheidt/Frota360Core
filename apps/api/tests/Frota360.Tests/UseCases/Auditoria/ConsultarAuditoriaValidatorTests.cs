using Frota360.Application.DTOs.Auditoria.Request;
using Frota360.Application.UseCases.Auditoria.Validator;
using Frota360.Domain.Common;

namespace Frota360.Tests.UseCases.Auditoria
{
    public class ConsultarAuditoriaValidatorTests
    {
        private readonly ConsultarAuditoriaValidator _validator = new();

        [Fact]
        public void FiltroVazio_DeveSerValido()
        {
            // Os defaults do request (página 1, 25 por página) abrem a tela sem filtro nenhum.
            Assert.True(_validator.Validate(new ConsultarAuditoriaRequest()).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        [InlineData(999_999)]
        public void TamanhoDePaginaForaDoTeto_DeveSerInvalido(int tamanho)
        {
            var resultado = _validator.Validate(new ConsultarAuditoriaRequest { TamanhoPagina = tamanho });

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(ConsultarAuditoriaRequest.TamanhoPagina));
        }

        [Fact]
        public void PaginaZero_DeveSerInvalida()
        {
            Assert.False(_validator.Validate(new ConsultarAuditoriaRequest { Pagina = 0 }).IsValid);
        }

        [Fact]
        public void EntidadeForaDoVocabulario_DeveSerInvalida()
        {
            Assert.False(_validator.Validate(new ConsultarAuditoriaRequest { Entidade = "Empresa" }).IsValid);
        }

        [Fact]
        public void AcaoForaDoVocabulario_DeveSerInvalida()
        {
            Assert.False(_validator.Validate(new ConsultarAuditoriaRequest { Acao = "Logou" }).IsValid);
        }

        [Fact]
        public void EntidadeEAcaoConhecidas_DevemSerValidas()
        {
            var resultado = _validator.Validate(new ConsultarAuditoriaRequest
            {
                Entidade = EntidadesAuditadas.Rota,
                Acao = AcoesAuditoria.Encerrou
            });

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void DataFinalAnteriorAInicial_DeveSerInvalida()
        {
            var resultado = _validator.Validate(new ConsultarAuditoriaRequest
            {
                De = new DateTime(2026, 8, 28),
                Ate = new DateTime(2026, 8, 1)
            });

            Assert.False(resultado.IsValid);
        }

        [Fact]
        public void PeriodoDeUmDiaSo_DeveSerValido()
        {
            // De == Ate é o caso comum "só hoje"; o repositório estende Ate até o fim do dia.
            var dia = new DateTime(2026, 8, 28);

            Assert.True(_validator.Validate(new ConsultarAuditoriaRequest { De = dia, Ate = dia }).IsValid);
        }
    }
}
