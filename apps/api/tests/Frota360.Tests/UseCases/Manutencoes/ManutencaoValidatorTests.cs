using Frota360.Application.DTOs.Manutencao.Request;
using Frota360.Application.UseCases.Manutencoes.Validator;

namespace Frota360.Tests.UseCases.Manutencoes
{
    public class ManutencaoValidatorTests
    {
        private readonly CreateManutencaoValidator _create = new();
        private readonly ConcluirManutencaoValidator _concluir = new();

        private static CreateManutencaoRequest CreateValido() => new()
        {
            VeiculoId = 5,
            TipoManutencaoId = 2,
            QuilometragemPrevista = 50_000
        };

        private static ConcluirManutencaoRequest ConcluirValido() => new()
        {
            QuilometragemRealizada = 50_100,
            DataRealizacao = DateTime.Now.Date,
            Custo = 350m
        };

        [Fact]
        public void Create_SoComKm_DeveAceitar()
        {
            Assert.True(_create.Validate(CreateValido()).IsValid);
        }

        [Fact]
        public void Create_ComDataPrevistaHoje_DeveAceitar()
        {
            var request = CreateValido();
            request.DataPrevista = DateTime.Now.Date;

            Assert.True(_create.Validate(request).IsValid);
        }

        [Fact]
        public void Create_ComDataPrevistaNoPassado_DeveRecusar()
        {
            var request = CreateValido();
            request.DataPrevista = DateTime.Now.Date.AddDays(-1);

            Assert.Contains(_create.Validate(request).Errors, e => e.PropertyName == "DataPrevista");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2_000_001)]
        public void Create_ComKmPrevistoForaDaFaixa_DeveRecusar(int km)
        {
            var request = CreateValido();
            request.QuilometragemPrevista = km;

            Assert.Contains(_create.Validate(request).Errors, e => e.PropertyName == "QuilometragemPrevista");
        }

        [Fact]
        public void Create_SemVeiculoNemTipo_DeveRecusarOsDois()
        {
            var request = CreateValido();
            request.VeiculoId = 0;
            request.TipoManutencaoId = 0;

            var erros = _create.Validate(request).Errors.Select(e => e.PropertyName).ToList();

            Assert.Contains("VeiculoId", erros);
            Assert.Contains("TipoManutencaoId", erros);
        }

        [Fact]
        public void Create_ComObservacaoLonga_DeveRecusar()
        {
            var request = CreateValido();
            request.Observacao = new string('x', 501);

            Assert.False(_create.Validate(request).IsValid);
        }

        [Fact]
        public void Concluir_ComDadosValidos_DeveAceitar()
        {
            Assert.True(_concluir.Validate(ConcluirValido()).IsValid);
        }

        [Fact]
        public void Concluir_SemCusto_DeveAceitar()
        {
            var request = ConcluirValido();
            request.Custo = null;

            Assert.True(_concluir.Validate(request).IsValid);
        }

        [Fact]
        public void Concluir_AmanhaPelaMargemDoFuso_DeveAceitar()
        {
            var request = ConcluirValido();
            request.DataRealizacao = DateTime.Now.Date.AddDays(1);

            Assert.True(_concluir.Validate(request).IsValid);
        }

        [Fact]
        public void Concluir_ComDataFutura_DeveRecusar()
        {
            var request = ConcluirValido();
            request.DataRealizacao = DateTime.Now.Date.AddDays(2);

            Assert.Contains(_concluir.Validate(request).Errors, e => e.PropertyName == "DataRealizacao");
        }

        [Fact]
        public void Concluir_ComCustoNegativo_DeveRecusar()
        {
            var request = ConcluirValido();
            request.Custo = -1m;

            Assert.Contains(_concluir.Validate(request).Errors, e => e.PropertyName == "Custo");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2_000_001)]
        public void Concluir_ComKmForaDaFaixa_DeveRecusar(int km)
        {
            var request = ConcluirValido();
            request.QuilometragemRealizada = km;

            Assert.Contains(_concluir.Validate(request).Errors, e => e.PropertyName == "QuilometragemRealizada");
        }
    }
}
