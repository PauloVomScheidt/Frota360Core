using Frota360.Application.DTOs.Custo.Request;
using Frota360.Application.UseCases.Custos;
using Frota360.Domain.Enums;
using Frota360.Domain.ReadModels;

namespace Frota360.Tests.UseCases.Custos
{
    public class CustoMappingsTests
    {
        [Fact]
        public void ToResponse_DeveLevarAOrigemComoTextoEPreservarOIdDeOrigem()
        {
            var lancamento = new LancamentoCusto(
                OrigemCusto.Manutencao, 42, new DateTime(2026, 8, 30),
                7, "Scania R450", "ABC1D23",
                null, null, "Troca de óleo", 1_200m, "Revisão programada");

            var resposta = lancamento.ToResponse();

            Assert.Equal("Manutencao", resposta.Origem);
            Assert.Equal(42, resposta.OrigemId);
            Assert.Equal("Troca de óleo", resposta.Categoria);
            Assert.Equal(1_200m, resposta.Valor);
            Assert.Null(resposta.MotoristaId);
            Assert.Null(resposta.MotoristaNome);
        }

        [Fact]
        public void ToResponse_DeveDesnormalizarVeiculoEMotoristaDoAbastecimento()
        {
            var lancamento = new LancamentoCusto(
                OrigemCusto.Abastecimento, 9, new DateTime(2026, 8, 30),
                7, "Scania R450", "ABC1D23",
                10, "Ana Souza", "Combustível", 250m, null);

            var resposta = lancamento.ToResponse();

            Assert.Equal("Abastecimento", resposta.Origem);
            Assert.Equal("ABC1D23", resposta.VeiculoPlaca);
            Assert.Equal(10, resposta.MotoristaId);
            Assert.Equal("Ana Souza", resposta.MotoristaNome);
        }

        [Theory]
        [InlineData("Abastecimento", OrigemCusto.Abastecimento)]
        [InlineData("manutencao", OrigemCusto.Manutencao)]
        public void ParaFiltro_DeveConverterAOrigemIgnorandoCaixa(string texto, OrigemCusto esperada)
        {
            var filtro = new ConsultarCustosRequest { Origem = texto }.ParaFiltro();

            Assert.Equal(esperada, filtro.Origem);
        }

        [Fact]
        public void ParaFiltro_ComOrigemDesconhecida_DeveFicarSemRecorteDeOrigem()
        {
            var filtro = new ResumoCustosRequest { Origem = "Pedagio" }.ParaFiltro();

            Assert.Null(filtro.Origem);
        }
    }
}
