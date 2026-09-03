using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Custo.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Enums;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.ReadModels;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Custos.Queries.GetResumoCustos
{
    /// <summary>
    /// O repositório soma uma origem por vez, no banco; aqui as origens são pivotadas em
    /// colunas e o custo por km é derivado. A divisão fica fora do SQL de propósito: é onde
    /// mora o zero no denominador.
    /// </summary>
    public sealed class GetResumoCustosHandler(ICustoRepository repository,
                                               ICurrentUserService currentUser,
                                               ILogger<GetResumoCustosHandler> logger)
        : IQueryHandler<GetResumoCustosQuery, ResumoCustosResponse>
    {
        public async Task<ResumoCustosResponse> HandleAsync(
            GetResumoCustosQuery query, CancellationToken cancellationToken = default)
        {
            var request = query.Data;

            logger.LogInformation("Resumindo custos | Veículo {VeiculoId} | Motorista {MotoristaId} | Origem {Origem} | De {De} | Até {Ate}",
                request.VeiculoId, request.MotoristaId, request.Origem, request.De, request.Ate);

            try
            {
                var empresaId = currentUser.EmpresaId;
                var filtro = request.ParaFiltro();

                var porVeiculo = await repository.SomarPorVeiculoAsync(empresaId, filtro);
                var porMes = await repository.SomarPorMesAsync(empresaId, filtro);
                var kmPorVeiculo = await repository.SomarKmPorVeiculoAsync(empresaId, filtro);
                var semCusto = await repository.ContarManutencoesSemCustoAsync(empresaId, filtro);

                var veiculos = PivotarVeiculos(porVeiculo, kmPorVeiculo);
                var meses = PivotarMeses(porMes);

                var totalAbastecimento = veiculos.Sum(v => v.TotalAbastecimento);
                var totalManutencao = veiculos.Sum(v => v.TotalManutencao);
                var totalDespesa = veiculos.Sum(v => v.TotalDespesa);
                var total = totalAbastecimento + totalManutencao + totalDespesa;
                var kmTotal = veiculos.Sum(v => v.Km);

                logger.LogInformation("Custos resumidos. Total {Total} em {Veiculos} veículos, {SemCusto} manutenções sem custo informado",
                    total, veiculos.Count, semCusto);

                return new ResumoCustosResponse
                {
                    Total = total,
                    TotalAbastecimento = totalAbastecimento,
                    TotalManutencao = totalManutencao,
                    TotalDespesa = totalDespesa,
                    QuantidadeLancamentos = porVeiculo.Sum(v => v.Quantidade),
                    KmTotal = kmTotal,
                    CustoPorKm = PorKm(total, kmTotal),
                    ManutencoesSemCustoInformado = semCusto,
                    PorVeiculo = veiculos,
                    PorMes = meses
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao resumir custos");
                throw;
            }
        }

        /// <summary>
        /// Junta as duas origens e o km na mesma linha. Veículo que rodou no período sem custo
        /// lançado entra com total zero em vez de sumir — é justamente o caso que merece ser
        /// visto, e assim as colunas fecham com os totais gerais.
        /// </summary>
        private static List<CustoPorVeiculoResponse> PivotarVeiculos(
            IEnumerable<TotalCustoPorVeiculo> totais, IEnumerable<KmPorVeiculo> quilometragens)
        {
            var km = quilometragens.ToDictionary(k => k.VeiculoId);

            var linhas = totais
                .GroupBy(t => t.VeiculoId)
                .Select(g =>
                {
                    var totalAbastecimento = g.Where(t => t.Origem == OrigemCusto.Abastecimento).Sum(t => t.Total);
                    var totalManutencao = g.Where(t => t.Origem == OrigemCusto.Manutencao).Sum(t => t.Total);
                    var totalDespesa = g.Where(t => t.Origem == OrigemCusto.Despesa).Sum(t => t.Total);
                    var totalDoVeiculo = totalAbastecimento + totalManutencao + totalDespesa;
                    var quilometragem = km.TryGetValue(g.Key, out var k) ? k.Km : 0;

                    return new CustoPorVeiculoResponse
                    {
                        VeiculoId = g.Key,
                        VeiculoNome = g.First().VeiculoNome,
                        VeiculoPlaca = g.First().VeiculoPlaca,
                        TotalAbastecimento = totalAbastecimento,
                        TotalManutencao = totalManutencao,
                        TotalDespesa = totalDespesa,
                        Total = totalDoVeiculo,
                        Km = quilometragem,
                        CustoPorKm = PorKm(totalDoVeiculo, quilometragem)
                    };
                })
                .ToList();

            var comCusto = linhas.Select(l => l.VeiculoId).ToHashSet();

            linhas.AddRange(km.Values
                .Where(k => !comCusto.Contains(k.VeiculoId))
                .Select(k => new CustoPorVeiculoResponse
                {
                    VeiculoId = k.VeiculoId,
                    VeiculoNome = k.VeiculoNome,
                    VeiculoPlaca = k.VeiculoPlaca,
                    Km = k.Km
                }));

            return [.. linhas.OrderByDescending(l => l.Total).ThenBy(l => l.VeiculoNome)];
        }

        private static List<CustoPorMesResponse> PivotarMeses(IEnumerable<TotalCustoPorMes> totais)
            => [.. totais
                .GroupBy(t => new { t.Ano, t.Mes })
                .Select(g => new CustoPorMesResponse
                {
                    Ano = g.Key.Ano,
                    Mes = g.Key.Mes,
                    TotalAbastecimento = g.Where(t => t.Origem == OrigemCusto.Abastecimento).Sum(t => t.Total),
                    TotalManutencao = g.Where(t => t.Origem == OrigemCusto.Manutencao).Sum(t => t.Total),
                    TotalDespesa = g.Where(t => t.Origem == OrigemCusto.Despesa).Sum(t => t.Total),
                    Total = g.Sum(t => t.Total)
                })
                .OrderBy(m => m.Ano).ThenBy(m => m.Mes)];

        /// <summary>Sem rota encerrada no período não há denominador — e o custo por km não existe.</summary>
        private static decimal? PorKm(decimal total, int km)
            => km > 0 ? Math.Round(total / km, 2, MidpointRounding.AwayFromZero) : null;
    }
}
