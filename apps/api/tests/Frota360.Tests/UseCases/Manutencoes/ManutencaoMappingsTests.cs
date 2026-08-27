using Frota360.Application.UseCases.Manutencoes;
using Frota360.Domain.Entities;
using Frota360.Domain.Enums;

namespace Frota360.Tests.UseCases.Manutencoes
{
    /// <summary>
    /// "Atrasada" e "km restantes" são derivados na leitura, comparando o previsto com a
    /// quilometragem atual do veículo — não existem como coluna. Estes testes fixam essa regra.
    /// </summary>
    public class ManutencaoMappingsTests
    {
        private static Manutencao Manutencao(int prevista, int kmVeiculo,
                                             StatusManutencao status = StatusManutencao.Pendente,
                                             DateTime? dataPrevista = null) => new()
        {
            Id = 1,
            QuilometragemPrevista = prevista,
            DataPrevista = dataPrevista,
            Status = status,
            Veiculo = new Veiculo { Id = 1, NomeVeiculo = "Fit", Placa = "ABC1D23", Quilometragem = kmVeiculo },
            Tipo = new TipoManutencao { Id = 1, Nome = "Troca de óleo" }
        };

        [Fact]
        public void QuandoFaltaRodar_NaoEstaAtrasadaEInformaOQueFalta()
        {
            var resposta = Manutencao(prevista: 60_000, kmVeiculo: 52_500).ToResponse();

            Assert.False(resposta.Atrasada);
            Assert.Equal(7_500, resposta.KmRestantes);
            Assert.Equal(52_500, resposta.QuilometragemAtualVeiculo);
        }

        [Fact]
        public void QuandoOVeiculoAtingiuOKmPrevisto_EstaAtrasada()
        {
            var resposta = Manutencao(prevista: 60_000, kmVeiculo: 60_000).ToResponse();

            Assert.True(resposta.Atrasada);
            Assert.Equal(0, resposta.KmRestantes);
        }

        [Fact]
        public void QuandoOVeiculoPassouDoKmPrevisto_KmRestantesFicaNegativo()
        {
            var resposta = Manutencao(prevista: 60_000, kmVeiculo: 63_200).ToResponse();

            Assert.True(resposta.Atrasada);
            Assert.Equal(-3_200, resposta.KmRestantes);
        }

        [Fact]
        public void QuandoADataPrevistaVenceuAntesDoKm_EstaAtrasada()
        {
            // Vence no que vier primeiro: o km ainda não chegou, mas a data já passou.
            var resposta = Manutencao(prevista: 60_000, kmVeiculo: 52_000,
                                      dataPrevista: DateTime.UtcNow.Date.AddDays(-1)).ToResponse();

            Assert.True(resposta.Atrasada);
        }

        [Fact]
        public void QuandoJaRealizada_NaoEstaAtrasadaENaoTemKmRestantes()
        {
            var resposta = Manutencao(prevista: 60_000, kmVeiculo: 80_000, StatusManutencao.Realizada).ToResponse();

            Assert.False(resposta.Atrasada);
            Assert.Null(resposta.KmRestantes);
        }
    }
}
