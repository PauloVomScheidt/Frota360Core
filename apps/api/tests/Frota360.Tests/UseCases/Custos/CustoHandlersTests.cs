using Frota360.Application.DTOs.Custo.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Custos.Queries.GetCustos;
using Frota360.Application.UseCases.Custos.Queries.GetResumoCustos;
using Frota360.Domain.Common;
using Frota360.Domain.Enums;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Custos
{
    public class CustoHandlersTests
    {
        private readonly ICustoRepository _repository = Substitute.For<ICustoRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

        public CustoHandlersTests()
        {
            _currentUser.EmpresaId.Returns(1);
        }

        private GetCustosHandler CriarHandlerDeLista() =>
            new(_repository, _currentUser, NullLogger<GetCustosHandler>.Instance);

        private GetResumoCustosHandler CriarHandlerDeResumo() =>
            new(_repository, _currentUser, NullLogger<GetResumoCustosHandler>.Instance);

        private static LancamentoCusto NovoLancamento(
            OrigemCusto origem = OrigemCusto.Abastecimento,
            int origemId = 1,
            decimal valor = 250m) => new(
                origem, origemId, new DateTime(2026, 8, 30),
                7, "Scania R450", "ABC1D23",
                origem == OrigemCusto.Abastecimento ? 10 : null,
                origem == OrigemCusto.Abastecimento ? "Ana Souza" : null,
                origem == OrigemCusto.Abastecimento ? "Combustível" : "Troca de óleo",
                valor, null);

        /// <summary>
        /// O resumo consome quatro consultas; os testes que olham só uma delas ainda precisam
        /// das outras três respondendo algo.
        /// </summary>
        private void ConfigurarResumo(
            IEnumerable<TotalCustoPorVeiculo>? porVeiculo = null,
            IEnumerable<TotalCustoPorMes>? porMes = null,
            IEnumerable<KmPorVeiculo>? km = null,
            int semCusto = 0)
        {
            _repository.SomarPorVeiculoAsync(Arg.Any<int>(), Arg.Any<FiltroCusto>())
                .Returns(porVeiculo ?? []);
            _repository.SomarPorMesAsync(Arg.Any<int>(), Arg.Any<FiltroCusto>())
                .Returns(porMes ?? []);
            _repository.SomarKmPorVeiculoAsync(Arg.Any<int>(), Arg.Any<FiltroCusto>())
                .Returns(km ?? []);
            _repository.ContarManutencoesSemCustoAsync(Arg.Any<int>(), Arg.Any<FiltroCusto>())
                .Returns(semCusto);
        }

        [Fact]
        public async Task Consultar_DeveEscoparNaEmpresaERepassarOFiltro()
        {
            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroCusto>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(([NovoLancamento()], 1));

            var handler = CriarHandlerDeLista();

            await handler.HandleAsync(new GetCustosQuery(new ConsultarCustosRequest
            {
                Pagina = 2,
                TamanhoPagina = 50,
                VeiculoId = 7,
                MotoristaId = 10,
                Origem = "Abastecimento",
                De = new DateTime(2026, 8, 1),
                Ate = new DateTime(2026, 8, 31)
            }));

            await _repository.Received(1).ConsultarAsync(1, Arg.Is<FiltroCusto>(f =>
                f.VeiculoId == 7
                && f.MotoristaId == 10
                && f.Origem == OrigemCusto.Abastecimento
                && f.De == new DateTime(2026, 8, 1)
                && f.Ate == new DateTime(2026, 8, 31)), 2, 50);
        }

        [Fact]
        public async Task Consultar_DeveMontarAPaginaComTotalETotalDePaginas()
        {
            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroCusto>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(([NovoLancamento()], 51));

            var handler = CriarHandlerDeLista();

            var pagina = await handler.HandleAsync(new GetCustosQuery(new ConsultarCustosRequest
            {
                Pagina = 3,
                TamanhoPagina = 25
            }));

            Assert.Equal(3, pagina.Pagina);
            Assert.Equal(25, pagina.TamanhoPagina);
            Assert.Equal(51, pagina.Total);
            Assert.Equal(3, pagina.TotalPaginas);
            Assert.Single(pagina.Itens);
        }

        [Fact]
        public async Task Consultar_ComOrigemInvalida_DeveConsultarSemFiltroDeOrigem()
        {
            // O validator barra antes de chegar aqui; se algo escapar, o pior resultado
            // possível é a consulta sem recorte de origem — nunca uma exceção.
            _repository.ConsultarAsync(Arg.Any<int>(), Arg.Any<FiltroCusto>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(([], 0));

            var handler = CriarHandlerDeLista();

            await handler.HandleAsync(new GetCustosQuery(new ConsultarCustosRequest { Origem = "Pedagio" }));

            await _repository.Received(1).ConsultarAsync(1,
                Arg.Is<FiltroCusto>(f => f.Origem == null), Arg.Any<int>(), Arg.Any<int>());
        }

        [Fact]
        public async Task Resumo_DeveEscoparNaEmpresaEmTodasAsConsultas()
        {
            ConfigurarResumo();

            var handler = CriarHandlerDeResumo();

            await handler.HandleAsync(new GetResumoCustosQuery(new ResumoCustosRequest()));

            await _repository.Received(1).SomarPorVeiculoAsync(1, Arg.Any<FiltroCusto>());
            await _repository.Received(1).SomarPorMesAsync(1, Arg.Any<FiltroCusto>());
            await _repository.Received(1).SomarKmPorVeiculoAsync(1, Arg.Any<FiltroCusto>());
            await _repository.Received(1).ContarManutencoesSemCustoAsync(1, Arg.Any<FiltroCusto>());
        }

        [Fact]
        public async Task Resumo_DevePivotarAsOrigensDoMesmoVeiculoEmUmaLinha()
        {
            // As três origens do mesmo veículo viram uma linha com três colunas.
            ConfigurarResumo(
                porVeiculo:
                [
                    new(7, "Scania R450", "ABC1D23", OrigemCusto.Abastecimento, 800m, 4),
                    new(7, "Scania R450", "ABC1D23", OrigemCusto.Manutencao, 1_200m, 1),
                    new(7, "Scania R450", "ABC1D23", OrigemCusto.Despesa, 400m, 2)
                ],
                km: [new(7, "Scania R450", "ABC1D23", 2_400, 3)]);

            var handler = CriarHandlerDeResumo();

            var resumo = await handler.HandleAsync(new GetResumoCustosQuery(new ResumoCustosRequest()));

            var veiculo = Assert.Single(resumo.PorVeiculo);
            Assert.Equal(800m, veiculo.TotalAbastecimento);
            Assert.Equal(1_200m, veiculo.TotalManutencao);
            Assert.Equal(400m, veiculo.TotalDespesa);
            Assert.Equal(2_400m, veiculo.Total);
            Assert.Equal(2_400, veiculo.Km);
            Assert.Equal(1.00m, veiculo.CustoPorKm);

            Assert.Equal(2_400m, resumo.Total);
            Assert.Equal(800m, resumo.TotalAbastecimento);
            Assert.Equal(1_200m, resumo.TotalManutencao);
            Assert.Equal(400m, resumo.TotalDespesa);
            Assert.Equal(7, resumo.QuantidadeLancamentos);
        }

        [Fact]
        public async Task Resumo_SemRotaEncerrada_DeveDevolverCustoPorKmNulo()
        {
            // Nenhuma rota encerrada no período: não há denominador, e devolver zero seria
            // afirmar que a frota rodou de graça.
            ConfigurarResumo(
                porVeiculo: [new(7, "Scania R450", "ABC1D23", OrigemCusto.Abastecimento, 500m, 2)]);

            var handler = CriarHandlerDeResumo();

            var resumo = await handler.HandleAsync(new GetResumoCustosQuery(new ResumoCustosRequest()));

            Assert.Null(resumo.CustoPorKm);
            Assert.Null(Assert.Single(resumo.PorVeiculo).CustoPorKm);
            Assert.Equal(0, resumo.KmTotal);
        }

        [Fact]
        public async Task Resumo_DeveCalcularOCustoPorKmDaFrotaSobreOKmTotal()
        {
            ConfigurarResumo(
                porVeiculo:
                [
                    new(7, "Scania R450", "ABC1D23", OrigemCusto.Abastecimento, 600m, 3),
                    new(8, "Volvo FH", "DEF2G34", OrigemCusto.Abastecimento, 400m, 2)
                ],
                km:
                [
                    new(7, "Scania R450", "ABC1D23", 3_000, 2),
                    new(8, "Volvo FH", "DEF2G34", 1_000, 1)
                ]);

            var handler = CriarHandlerDeResumo();

            var resumo = await handler.HandleAsync(new GetResumoCustosQuery(new ResumoCustosRequest()));

            Assert.Equal(4_000, resumo.KmTotal);
            Assert.Equal(0.25m, resumo.CustoPorKm);
        }

        [Fact]
        public async Task Resumo_DeveIncluirVeiculoQueRodouSemCustoLancado()
        {
            // Rodou 5.000 km e não teve um abastecimento lançado: é o caso que mais merece
            // ser visto, e mantê-lo faz as colunas fecharem com o km total.
            ConfigurarResumo(
                porVeiculo: [new(7, "Scania R450", "ABC1D23", OrigemCusto.Abastecimento, 600m, 3)],
                km:
                [
                    new(7, "Scania R450", "ABC1D23", 3_000, 2),
                    new(9, "Iveco Tector", "GHI3J45", 5_000, 4)
                ]);

            var handler = CriarHandlerDeResumo();

            var resumo = await handler.HandleAsync(new GetResumoCustosQuery(new ResumoCustosRequest()));

            var semCusto = Assert.Single(resumo.PorVeiculo, v => v.VeiculoId == 9);
            Assert.Equal(0m, semCusto.Total);
            Assert.Equal(5_000, semCusto.Km);
            Assert.Null(semCusto.CustoPorKm);
            Assert.Equal(8_000, resumo.KmTotal);
        }

        [Fact]
        public async Task Resumo_DeveOrdenarVeiculosDoMaiorTotalParaOMenor()
        {
            ConfigurarResumo(porVeiculo:
            [
                new(7, "Scania R450", "ABC1D23", OrigemCusto.Abastecimento, 100m, 1),
                new(8, "Volvo FH", "DEF2G34", OrigemCusto.Abastecimento, 900m, 1),
                new(9, "Iveco Tector", "GHI3J45", OrigemCusto.Manutencao, 500m, 1)
            ]);

            var handler = CriarHandlerDeResumo();

            var resumo = await handler.HandleAsync(new GetResumoCustosQuery(new ResumoCustosRequest()));

            Assert.Equal([8, 9, 7], resumo.PorVeiculo.Select(v => v.VeiculoId));
        }

        [Fact]
        public async Task Resumo_DevePivotarOsMesesEOrdenarCronologicamente()
        {
            ConfigurarResumo(porMes:
            [
                new(2026, 8, OrigemCusto.Manutencao, 300m),
                new(2026, 7, OrigemCusto.Abastecimento, 100m),
                new(2026, 8, OrigemCusto.Abastecimento, 200m),
                new(2026, 8, OrigemCusto.Despesa, 50m)
            ]);

            var handler = CriarHandlerDeResumo();

            var resumo = await handler.HandleAsync(new GetResumoCustosQuery(new ResumoCustosRequest()));

            Assert.Equal([(2026, 7), (2026, 8)], resumo.PorMes.Select(m => (m.Ano, m.Mes)));
            Assert.Equal(550m, resumo.PorMes[1].Total);
            Assert.Equal(200m, resumo.PorMes[1].TotalAbastecimento);
            Assert.Equal(300m, resumo.PorMes[1].TotalManutencao);
            Assert.Equal(50m, resumo.PorMes[1].TotalDespesa);
        }

        [Fact]
        public async Task Resumo_DeveRepassarAContagemDeManutencoesSemCustoInformado()
        {
            // Elas ficam fora de toda soma; sem a contagem, o total mentiria por omissão.
            ConfigurarResumo(semCusto: 3);

            var handler = CriarHandlerDeResumo();

            var resumo = await handler.HandleAsync(new GetResumoCustosQuery(new ResumoCustosRequest()));

            Assert.Equal(3, resumo.ManutencoesSemCustoInformado);
        }
    }
}
