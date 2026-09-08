using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Enums;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.ReadModels;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    /// <summary>
    /// Não há tabela de custos. O custo mora na tabela de origem e aqui é unido na leitura,
    /// o que mantém uma fonte de verdade só: corrigir o valor no abastecimento já corrige o
    /// relatório, sem sincronia nenhuma para dar errado.
    ///
    /// São três origens. Abastecimento e manutenção têm tela própria e este read model
    /// apenas as lê; <c>Despesa</c> é diferente — ela existe porque o gasto não tinha onde
    /// morar, então ali a tabela é fonte de verdade. A distinção não aparece aqui: as três
    /// entram como <c>LancamentoCusto</c> e o resto do sistema não sabe a diferença.
    /// </summary>
    public class CustoRepository(Frota360DbContext context) : ICustoRepository
    {
        private const string CategoriaCombustivel = "Combustível";

        /// <summary>
        /// A união das duas origens é feita em memória, e não com <c>Concat</c>: o EF Core não
        /// traduz operação de conjunto depois de uma projeção com constantes (a origem e a
        /// categoria são literais), nem ordena por elas. Provado em
        /// <c>TraducaoDeConsultaTests</c> — se um dia passar a traduzir, aquele teste é o lugar
        /// de descobrir.
        ///
        /// O custo disso é limitado, não é "trazer tudo": nenhuma linha além da
        /// <c>pagina × tamanhoPagina</c>-ésima de cada origem pode entrar na página pedida, então
        /// cada consulta lê no máximo isso — e o validator ainda limita a página a 100.
        /// </summary>
        public async Task<(IEnumerable<LancamentoCusto> Itens, int Total)> ConsultarAsync(
            int empresaId, FiltroCusto filtro, int pagina, int tamanhoPagina)
        {
            var teto = pagina * tamanhoPagina;
            var lancamentos = new List<LancamentoCusto>();
            var total = 0;

            if (IncluirAbastecimentos(filtro))
            {
                var consulta = Abastecimentos(empresaId, filtro);
                total += await consulta.CountAsync();
                lancamentos.AddRange(await Projetar(consulta
                    .OrderByDescending(a => a.DataAbastecimento)
                    .ThenByDescending(a => a.Id)
                    .Take(teto)).ToListAsync());
            }

            if (IncluirManutencoes(filtro))
            {
                var consulta = Manutencoes(empresaId, filtro);
                total += await consulta.CountAsync();
                lancamentos.AddRange(await Projetar(consulta
                    .OrderByDescending(m => m.DataRealizacao)
                    .ThenByDescending(m => m.Id)
                    .Take(teto)).ToListAsync());
            }

            if (IncluirDespesas(filtro))
            {
                var consulta = Despesas(empresaId, filtro);
                total += await consulta.CountAsync();
                lancamentos.AddRange(await Projetar(consulta
                    .OrderByDescending(d => d.DataDespesa)
                    .ThenByDescending(d => d.Id)
                    .Take(teto)).ToListAsync());
            }

            var itens = lancamentos
                .OrderByDescending(l => l.Data)
                // Ids de tabelas diferentes colidem, então o desempate passa pela origem antes
                // do id — sem isso a ordem é instável e a paginação repete linhas.
                .ThenBy(l => l.Origem)
                .ThenByDescending(l => l.OrigemId)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToList();

            return (itens, total);
        }

        public async Task<IEnumerable<TotalCustoPorVeiculo>> SomarPorVeiculoAsync(int empresaId, FiltroCusto filtro)
        {
            var totais = new List<TotalCustoPorVeiculo>();

            if (IncluirAbastecimentos(filtro))
                totais.AddRange(await Abastecimentos(empresaId, filtro)
                    .GroupBy(a => new { a.VeiculoId, a.Veiculo!.NomeVeiculo, a.Veiculo!.Placa })
                    .Select(g => new TotalCustoPorVeiculo(
                        g.Key.VeiculoId, g.Key.NomeVeiculo, g.Key.Placa,
                        OrigemCusto.Abastecimento, g.Sum(a => a.Valor), g.Count()))
                    .ToListAsync());

            if (IncluirManutencoes(filtro))
                totais.AddRange(await Manutencoes(empresaId, filtro)
                    .GroupBy(m => new { m.VeiculoId, m.Veiculo!.NomeVeiculo, m.Veiculo!.Placa })
                    .Select(g => new TotalCustoPorVeiculo(
                        g.Key.VeiculoId, g.Key.NomeVeiculo, g.Key.Placa,
                        OrigemCusto.Manutencao, g.Sum(m => m.Custo!.Value), g.Count()))
                    .ToListAsync());

            if (IncluirDespesas(filtro))
                totais.AddRange(await Despesas(empresaId, filtro)
                    .GroupBy(d => new { d.VeiculoId, d.Veiculo!.NomeVeiculo, d.Veiculo!.Placa })
                    .Select(g => new TotalCustoPorVeiculo(
                        g.Key.VeiculoId, g.Key.NomeVeiculo, g.Key.Placa,
                        OrigemCusto.Despesa, g.Sum(d => d.Valor), g.Count()))
                    .ToListAsync());

            return totais;
        }

        public async Task<IEnumerable<TotalCustoPorMes>> SomarPorMesAsync(int empresaId, FiltroCusto filtro)
        {
            var totais = new List<TotalCustoPorMes>();

            if (IncluirAbastecimentos(filtro))
                totais.AddRange(await Abastecimentos(empresaId, filtro)
                    .GroupBy(a => new { a.DataAbastecimento.Year, a.DataAbastecimento.Month })
                    .Select(g => new TotalCustoPorMes(
                        g.Key.Year, g.Key.Month, OrigemCusto.Abastecimento, g.Sum(a => a.Valor)))
                    .ToListAsync());

            if (IncluirManutencoes(filtro))
                totais.AddRange(await Manutencoes(empresaId, filtro)
                    .GroupBy(m => new { m.DataRealizacao!.Value.Year, m.DataRealizacao!.Value.Month })
                    .Select(g => new TotalCustoPorMes(
                        g.Key.Year, g.Key.Month, OrigemCusto.Manutencao, g.Sum(m => m.Custo!.Value)))
                    .ToListAsync());

            if (IncluirDespesas(filtro))
                totais.AddRange(await Despesas(empresaId, filtro)
                    .GroupBy(d => new { d.DataDespesa.Year, d.DataDespesa.Month })
                    .Select(g => new TotalCustoPorMes(
                        g.Key.Year, g.Key.Month, OrigemCusto.Despesa, g.Sum(d => d.Valor)))
                    .ToListAsync());

            return totais;
        }

        public async Task<int> ContarManutencoesSemCustoAsync(int empresaId, FiltroCusto filtro)
        {
            if (!IncluirManutencoes(filtro))
                return 0;

            var consulta = context.Manutencoes.AsNoTracking()
                .Where(m => m.EmpresaId == empresaId
                         && m.Status == StatusManutencao.Realizada
                         && m.DataRealizacao != null
                         && m.Custo == null);

            if (filtro.VeiculoId is not null)
                consulta = consulta.Where(m => m.VeiculoId == filtro.VeiculoId);

            if (filtro.De is not null)
            {
                var inicio = filtro.De.Value.Date;
                consulta = consulta.Where(m => m.DataRealizacao >= inicio);
            }

            if (filtro.Ate is not null)
            {
                var fim = filtro.Ate.Value.Date.AddDays(1);
                consulta = consulta.Where(m => m.DataRealizacao < fim);
            }

            return await consulta.CountAsync();
        }

        /// <summary>
        /// Km apurado no período, das rotas <b>encerradas</b> — <c>KmPercorrido</c> é persistido
        /// no encerramento e nunca deve ser recalculado a partir de kmInicial/kmFinal.
        ///
        /// O recorte é por <c>DataFim</c>, o momento em que a quilometragem foi apurada, e a
        /// origem do custo é ignorada de propósito: o km rodado é o mesmo, seja qual for o
        /// tipo de gasto que está sendo dividido por ele.
        /// </summary>
        public async Task<IEnumerable<KmPorVeiculo>> SomarKmPorVeiculoAsync(int empresaId, FiltroCusto filtro)
        {
            var consulta = context.Rotas.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId
                         && r.DataFim != null
                         && r.KmPercorrido != null);

            if (filtro.VeiculoId is not null)
                consulta = consulta.Where(r => r.CodigoVeiculo == filtro.VeiculoId);

            if (filtro.MotoristaId is not null)
                consulta = consulta.Where(r => r.CodigoMotorista == filtro.MotoristaId);

            if (filtro.De is not null)
            {
                var inicio = filtro.De.Value.Date;
                consulta = consulta.Where(r => r.DataFim >= inicio);
            }

            if (filtro.Ate is not null)
            {
                var fim = filtro.Ate.Value.Date.AddDays(1);
                consulta = consulta.Where(r => r.DataFim < fim);
            }

            return await consulta
                .GroupBy(r => new { r.CodigoVeiculo, r.Veiculo!.NomeVeiculo, r.Veiculo!.Placa })
                .Select(g => new KmPorVeiculo(
                    g.Key.CodigoVeiculo, g.Key.NomeVeiculo, g.Key.Placa,
                    g.Sum(r => r.KmPercorrido!.Value), g.Count()))
                .ToListAsync();
        }

        /// <summary>
        /// Litros e km por veículo, para o km/l da tela de custos.
        ///
        /// <para>
        /// A agregação é feita <b>em memória</b>, sobre uma projeção de três colunas. Não é
        /// preguiça: o cálculo precisa dos litros do <b>primeiro</b> abastecimento de cada
        /// veículo para descontá-los, e isso é window function — exatamente o que o EF não
        /// traduz, a mesma lição que <c>TraducaoDeConsultaTests</c> guarda sobre a união das
        /// origens. O conjunto é limitado pelo período do filtro, então cabe.
        /// </para>
        /// </summary>
        public async Task<IEnumerable<ConsumoPorVeiculo>> SomarConsumoPorVeiculoAsync(int empresaId, FiltroCusto filtro)
        {
            var linhas = await Abastecimentos(empresaId, filtro)
                .Select(a => new { a.VeiculoId, a.Veiculo!.NomeVeiculo, a.Veiculo!.Placa, a.Odometro, a.Litros })
                .ToListAsync();

            return linhas
                .GroupBy(l => l.VeiculoId)
                .Select(g =>
                {
                    // Ordenar por odômetro e não por data: lançamento retroativo é aceito pelo
                    // sistema, então a ordem cronológica não é a ordem da estrada.
                    var porOdometro = g.OrderBy(l => l.Odometro).ToList();

                    var km = porOdometro[^1].Odometro - porOdometro[0].Odometro;

                    // Os litros do primeiro pagaram o trecho ANTERIOR ao período; incluí-los
                    // infla o denominador e subestima o km/l de forma sistemática.
                    var litros = porOdometro.Skip(1).Sum(l => l.Litros);

                    return new ConsumoPorVeiculo(
                        g.Key, porOdometro[0].NomeVeiculo, porOdometro[0].Placa,
                        litros, km, porOdometro.Count);
                })
                .ToList();
        }

        /// <summary>
        /// Sem <c>Include</c> de propósito: a navegação é lida <b>dentro</b> do <c>Select</c>,
        /// que o EF traduz para JOIN. Um <c>Include</c> aqui seria descartado em silêncio.
        /// </summary>
        private static IQueryable<LancamentoCusto> Projetar(IQueryable<Abastecimento> consulta)
            => consulta.Select(a => new LancamentoCusto(
                OrigemCusto.Abastecimento,
                a.Id,
                a.DataAbastecimento,
                a.VeiculoId,
                a.Veiculo!.NomeVeiculo,
                a.Veiculo!.Placa,
                a.MotoristaId,
                a.Motorista!.Nome,
                CategoriaCombustivel,
                a.Valor,
                a.Observacao));

        private static IQueryable<LancamentoCusto> Projetar(IQueryable<Manutencao> consulta)
            => consulta.Select(m => new LancamentoCusto(
                OrigemCusto.Manutencao,
                m.Id,
                m.DataRealizacao!.Value,
                m.VeiculoId,
                m.Veiculo!.NomeVeiculo,
                m.Veiculo!.Placa,
                null,
                null,
                m.Tipo!.Nome,
                m.Custo!.Value,
                m.Observacao));

        private static IQueryable<LancamentoCusto> Projetar(IQueryable<Despesa> consulta)
            => consulta.Select(d => new LancamentoCusto(
                OrigemCusto.Despesa,
                d.Id,
                d.DataDespesa,
                d.VeiculoId,
                d.Veiculo!.NomeVeiculo,
                d.Veiculo!.Placa,
                d.MotoristaId,
                d.Motorista!.Nome,
                d.Tipo!.Nome,
                d.Valor,
                d.Observacao));

        private IQueryable<Abastecimento> Abastecimentos(int empresaId, FiltroCusto filtro)
        {
            var consulta = context.Abastecimentos.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId);

            if (filtro.VeiculoId is not null)
                consulta = consulta.Where(a => a.VeiculoId == filtro.VeiculoId);

            if (filtro.MotoristaId is not null)
                consulta = consulta.Where(a => a.MotoristaId == filtro.MotoristaId);

            if (filtro.De is not null)
            {
                var inicio = filtro.De.Value.Date;
                consulta = consulta.Where(a => a.DataAbastecimento >= inicio);
            }

            // "Até" é inclusivo, como no resto do sistema: quem escolhe 31/08 espera ver o
            // que aconteceu às 23h daquele dia.
            if (filtro.Ate is not null)
            {
                var fim = filtro.Ate.Value.Date.AddDays(1);
                consulta = consulta.Where(a => a.DataAbastecimento < fim);
            }

            return consulta;
        }

        /// <summary>
        /// Só manutenção concluída e com custo informado é custo realizado. Pendente e
        /// cancelada não entram, e a concluída sem custo é contada à parte para virar aviso
        /// na tela.
        /// </summary>
        private IQueryable<Manutencao> Manutencoes(int empresaId, FiltroCusto filtro)
        {
            var consulta = context.Manutencoes.AsNoTracking()
                .Where(m => m.EmpresaId == empresaId
                         && m.Status == StatusManutencao.Realizada
                         && m.Custo != null
                         && m.DataRealizacao != null);

            if (filtro.VeiculoId is not null)
                consulta = consulta.Where(m => m.VeiculoId == filtro.VeiculoId);

            if (filtro.De is not null)
            {
                var inicio = filtro.De.Value.Date;
                consulta = consulta.Where(m => m.DataRealizacao >= inicio);
            }

            if (filtro.Ate is not null)
            {
                var fim = filtro.Ate.Value.Date.AddDays(1);
                consulta = consulta.Where(m => m.DataRealizacao < fim);
            }

            return consulta;
        }

        /// <summary>
        /// Despesa tem motorista quando é de alguém (multa), então — ao contrário da
        /// manutenção — filtrar por motorista <b>não</b> a descarta: ela é recortada pela
        /// coluna, e as despesas sem dono (IPVA, seguro) é que ficam de fora.
        /// </summary>
        private IQueryable<Despesa> Despesas(int empresaId, FiltroCusto filtro)
        {
            var consulta = context.Despesas.AsNoTracking()
                .Where(d => d.EmpresaId == empresaId);

            if (filtro.VeiculoId is not null)
                consulta = consulta.Where(d => d.VeiculoId == filtro.VeiculoId);

            if (filtro.MotoristaId is not null)
                consulta = consulta.Where(d => d.MotoristaId == filtro.MotoristaId);

            if (filtro.De is not null)
            {
                var inicio = filtro.De.Value.Date;
                consulta = consulta.Where(d => d.DataDespesa >= inicio);
            }

            if (filtro.Ate is not null)
            {
                var fim = filtro.Ate.Value.Date.AddDays(1);
                consulta = consulta.Where(d => d.DataDespesa < fim);
            }

            return consulta;
        }

        private static bool IncluirAbastecimentos(FiltroCusto filtro)
            => filtro.Origem is null or OrigemCusto.Abastecimento;

        /// <summary>
        /// Filtrar por motorista descarta a manutenção inteira: ela não é atribuída a
        /// motorista no modelo, e deduzi-la pela rota do veículo seria um chute. É a única
        /// das três origens que some nesse recorte.
        /// </summary>
        private static bool IncluirManutencoes(FiltroCusto filtro)
            => filtro.MotoristaId is null && filtro.Origem is null or OrigemCusto.Manutencao;

        private static bool IncluirDespesas(FiltroCusto filtro)
            => filtro.Origem is null or OrigemCusto.Despesa;
    }
}
