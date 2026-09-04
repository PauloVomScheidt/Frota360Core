using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Enums;
using Frota360.Infrastructure.Data;
using Frota360.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Frota360.IntegrationTests
{
    /// <summary>
    /// Consultas cuja tradução para SQL muda entre provedores: filtro por intervalo de datas,
    /// enum persistido como texto e o <c>ORDER BY</c> com condicional. Rodam contra o banco de
    /// verdade porque é a tradução — e não o LINQ — que está sob teste.
    /// </summary>
    [Collection(BancoCollection.Nome)]
    public class TraducaoDeConsultaTests(BancoFixture fixture)
    {
        private sealed record Cenario(int EmpresaId, int VeiculoId, int MotoristaId, int TipoId,
                                      int TipoCombustivelId, int PostoId);

        /// <summary>
        /// Filtro de abastecimento do cenário. Página grande por padrão — a maioria dos testes
        /// daqui é sobre o `Where`, não sobre a paginação; quem testa paginação passa os dois.
        /// </summary>
        private static FiltroAbastecimento Filtro(Cenario c, int pagina = 1, int tamanhoPagina = 50,
            DateTime? de = null, DateTime? ate = null)
            => new(pagina, tamanhoPagina, VeiculoId: null, MotoristaId: null, De: de, Ate: ate);

        private async Task<Cenario> MontarAsync(Frota360DbContext contexto)
        {
            var empresa = new Empresa { Nome = Unicos.Texto("Empresa"), DataInclusao = DateTime.Now };
            contexto.Empresas.Add(empresa);
            await contexto.SaveChangesAsync();

            var veiculo = new Veiculo
            {
                EmpresaId = empresa.Id,
                NomeVeiculo = "Scania R450",
                MarcaVeiculo = "Scania",
                Placa = Unicos.Placa(),
                Quilometragem = 100_000,
                DataInclusao = DateTime.Now
            };
            var motorista = new Usuario
            {
                EmpresaId = empresa.Id,
                Nome = "Motorista",
                Email = Unicos.Email("mot"),
                SenhaHash = "$2a$11$abcdefghijklmnopqrstuv",
                Role = Domain.Common.Roles.Motorista,
                DataInclusao = DateTime.Now
            };
            var tipo = new TipoManutencao
            {
                EmpresaId = empresa.Id,
                Nome = Unicos.Texto("Troca de oleo"),
                IntervaloKm = 10_000,
                DataInclusao = DateTime.Now
            };
            var combustivel = new TipoCombustivel
            {
                EmpresaId = empresa.Id,
                Nome = Unicos.Texto("Diesel S10"),
                DataInclusao = DateTime.Now
            };
            var posto = new Posto
            {
                EmpresaId = empresa.Id,
                Nome = Unicos.Texto("Posto Ipiranga"),
                DataInclusao = DateTime.Now
            };
            contexto.Veiculos.Add(veiculo);
            contexto.Usuarios.Add(motorista);
            contexto.TiposManutencao.Add(tipo);
            contexto.TiposCombustivel.Add(combustivel);
            contexto.Postos.Add(posto);
            await contexto.SaveChangesAsync();

            return new Cenario(empresa.Id, veiculo.Id, motorista.Id, tipo.Id, combustivel.Id, posto.Id);
        }

        [Fact]
        public async Task Consumo_DeveDescontarOsLitrosDoPrimeiroAbastecimentoDoPeriodo()
        {
            // A agregação roda em memória justamente porque precisa dos litros do primeiro
            // abastecimento — window function, que o EF não traduz. Este teste prova que a
            // projeção que a alimenta traduz e que a conta fecha contra o banco de verdade.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            contexto.Abastecimentos.AddRange(
                NovoAbastecimento(c, new DateTime(2026, 8, 10), 300m, odometro: 100_000, litros: 45m),
                NovoAbastecimento(c, new DateTime(2026, 8, 20), 320m, odometro: 100_500, litros: 50m));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var repositorio = new CustoRepository(contexto);
            var consumo = (await repositorio.SomarConsumoPorVeiculoAsync(c.EmpresaId, new FiltroCusto())).Single();

            Assert.Equal(500, consumo.Km);
            // 50, não 95: os 45 do primeiro pagaram o trecho anterior ao período.
            Assert.Equal(50m, consumo.Litros);
            Assert.Equal(2, consumo.Abastecimentos);
            Assert.Equal("Scania R450", consumo.VeiculoNome);
        }

        [Fact]
        public async Task Consumo_ComAbastecimentoLancadoForaDeOrdem_NaoDeveDarKmNegativo()
        {
            // Odômetro menor é aceito pelo sistema, então a ordem cronológica não é a ordem
            // da estrada: a agregação ordena por odômetro justamente por isso.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            contexto.Abastecimentos.AddRange(
                NovoAbastecimento(c, new DateTime(2026, 8, 20), 320m, odometro: 100_500, litros: 50m),
                NovoAbastecimento(c, new DateTime(2026, 8, 25), 300m, odometro: 100_000, litros: 45m));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var repositorio = new CustoRepository(contexto);
            var consumo = (await repositorio.SomarConsumoPorVeiculoAsync(c.EmpresaId, new FiltroCusto())).Single();

            Assert.Equal(500, consumo.Km);
            Assert.Equal(50m, consumo.Litros);
        }

        [Fact]
        public async Task Consumo_ComUmAbastecimentoSo_NaoDeveTerIntervalo()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            contexto.Abastecimentos.Add(
                NovoAbastecimento(c, new DateTime(2026, 8, 20), 320m, odometro: 100_500, litros: 50m));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var repositorio = new CustoRepository(contexto);
            var consumo = (await repositorio.SomarConsumoPorVeiculoAsync(c.EmpresaId, new FiltroCusto())).Single();

            Assert.Equal(0, consumo.Km);
            Assert.Equal(0m, consumo.Litros);
        }

        [Fact]
        public async Task Abastecimento_DeveCarregarOsNomesDeCombustivelEPosto()
        {
            // A listagem desnormaliza os dois nomes. É exatamente a classe de bug que o
            // `Include` descartado em silêncio já causou aqui (listagem sem placa): se
            // ComIncludes deixar um dos catálogos de fora, o nome volta vazio.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            contexto.Abastecimentos.Add(NovoAbastecimento(c, new DateTime(2026, 8, 30), 320m));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var repositorio = new AbastecimentoRepository(contexto);
            var (itens, _) = await repositorio.ConsultarAsync(c.EmpresaId, Filtro(c));
            var lido = itens.Single();

            Assert.NotNull(lido.TipoCombustivel);
            Assert.NotNull(lido.Posto);
            Assert.StartsWith("Diesel S10", lido.TipoCombustivel!.Nome);
            Assert.StartsWith("Posto Ipiranga", lido.Posto!.Nome);
        }

        [Fact]
        public async Task FiltroDePeriodo_DeveIncluirOLancamentoDoProprioDiaFinal()
        {
            // `ate` é inclusivo: o repositório soma um dia e usa `<`. É o bug clássico de
            // filtro de data — quem lançou hoje tem de aparecer ao filtrar até hoje.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            contexto.Abastecimentos.AddRange(
                NovoAbastecimento(c, dia.AddDays(-1), 100m),
                NovoAbastecimento(c, dia, 200m),
                NovoAbastecimento(c, dia.AddDays(1), 300m));
            await contexto.SaveChangesAsync();

            var repositorio = new AbastecimentoRepository(contexto);
            var (noDia, total) = await repositorio.ConsultarAsync(c.EmpresaId, Filtro(c, de: dia, ate: dia));

            var valores = noDia.Select(a => a.Valor).ToList();
            Assert.Equal(1, total);
            Assert.Single(valores);
            Assert.Equal(200m, valores[0]);
        }

        [Fact]
        public async Task FiltroDePeriodo_DeveAbrangerOIntervaloInteiro()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            contexto.Abastecimentos.AddRange(
                NovoAbastecimento(c, dia.AddDays(-1), 100m),
                NovoAbastecimento(c, dia, 200m),
                NovoAbastecimento(c, dia.AddDays(1), 300m));
            await contexto.SaveChangesAsync();

            var repositorio = new AbastecimentoRepository(contexto);
            var (intervalo, total) = await repositorio.ConsultarAsync(c.EmpresaId,
                Filtro(c, de: dia.AddDays(-1), ate: dia.AddDays(1)));

            Assert.Equal(3, intervalo.Count());
            Assert.Equal(3, total);
        }

        [Fact]
        public async Task Status_DevePersistirComoTextoEVoltarComoEnum()
        {
            // HasConversion<string>: o banco fica legível e novos status não dependem da
            // ordem do enum. Se a conversão sumir, a coluna vira inteiro e o filtro quebra.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            contexto.Manutencoes.Add(NovaManutencao(c, StatusManutencao.Realizada, 110_000));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            // O EF projeta SqlQuery<T> sobre uma coluna chamada "Value" — daí o alias.
            var comoTexto = await contexto.Database
                .SqlQuery<string>($@"SELECT ""Status"" AS ""Value"" FROM ""Manutencao""
                                     WHERE ""EmpresaId"" = {c.EmpresaId}")
                .ToListAsync();
            Assert.Equal(["Realizada"], comoTexto);

            var repositorio = new ManutencaoRepository(contexto);
            var (filtradas, _) = await repositorio.ConsultarAsync(c.EmpresaId,
                new FiltroManutencao(1, 50, Status: StatusManutencao.Realizada));
            Assert.Single(filtradas);
        }

        [Fact]
        public async Task Ordenacao_DeveTrazerPendentesPrimeiro_MesmoComStatusEmTexto()
        {
            // O OrderBy vira CASE WHEN no SQL. Sem ele a ordem seria alfabética do texto
            // ("Cancelada", "Pendente", "Realizada") e a tela mostraria o irrelevante no topo.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            contexto.Manutencoes.AddRange(
                NovaManutencao(c, StatusManutencao.Realizada, 105_000),
                NovaManutencao(c, StatusManutencao.Pendente, 130_000),
                NovaManutencao(c, StatusManutencao.Cancelada, 101_000),
                NovaManutencao(c, StatusManutencao.Pendente, 120_000));
            await contexto.SaveChangesAsync();

            var repositorio = new ManutencaoRepository(contexto);
            var (pagina, _) = await repositorio.ConsultarAsync(c.EmpresaId, new FiltroManutencao(1, 50));
            var ordenadas = pagina.ToList();

            Assert.Equal(StatusManutencao.Pendente, ordenadas[0].Status);
            Assert.Equal(120_000, ordenadas[0].QuilometragemPrevista);
            Assert.Equal(StatusManutencao.Pendente, ordenadas[1].Status);
            Assert.Equal(130_000, ordenadas[1].QuilometragemPrevista);
            Assert.All(ordenadas.Skip(2), m => Assert.NotEqual(StatusManutencao.Pendente, m.Status));
        }

        [Fact]
        public async Task GetAll_DeveRecortarPelaEmpresa()
        {
            // A regra mais importante do sistema, provada no banco e não no mock.
            await using var contexto = fixture.CriarContexto();
            var minha = await MontarAsync(contexto);
            var outra = await MontarAsync(contexto);

            contexto.Abastecimentos.Add(NovoAbastecimento(minha, new DateTime(2026, 8, 30), 100m));
            contexto.Abastecimentos.Add(NovoAbastecimento(outra, new DateTime(2026, 8, 30), 999m));
            await contexto.SaveChangesAsync();

            var repositorio = new AbastecimentoRepository(contexto);
            var (meus, total) = await repositorio.ConsultarAsync(minha.EmpresaId, Filtro(minha));

            Assert.Single(meus);
            Assert.Equal(100m, meus.Single().Valor);
            // O COUNT da paginação também é recortado — senão o total vazaria o volume da outra.
            Assert.Equal(1, total);
        }

        /// <summary>
        /// A paginação prova-se no banco: `Skip`/`Take` sobre uma ordenação total. Sem o desempate
        /// por id, duas linhas do mesmo dia podem trocar de lugar entre as consultas e a página 2
        /// repetir o que a 1 mostrou.
        /// </summary>
        [Fact]
        public async Task Consultar_DevePaginarSemRepetirNemPularLinhas()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            // Todos no mesmo dia de propósito: é o caso em que só o desempate por id salva.
            for (var i = 0; i < 5; i++)
                contexto.Abastecimentos.Add(NovoAbastecimento(c, dia, 100m + i));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var repositorio = new AbastecimentoRepository(contexto);
            var (p1, total) = await repositorio.ConsultarAsync(c.EmpresaId, Filtro(c, pagina: 1, tamanhoPagina: 2));
            var (p2, _) = await repositorio.ConsultarAsync(c.EmpresaId, Filtro(c, pagina: 2, tamanhoPagina: 2));
            var (p3, _) = await repositorio.ConsultarAsync(c.EmpresaId, Filtro(c, pagina: 3, tamanhoPagina: 2));

            Assert.Equal(5, total);           // o total ignora a paginação
            Assert.Equal(2, p1.Count());
            Assert.Equal(2, p2.Count());
            Assert.Single(p3);                // a última página é a parcial

            var ids = p1.Concat(p2).Concat(p3).Select(a => a.Id).ToList();
            Assert.Equal(5, ids.Distinct().Count());   // nenhuma linha repetida entre páginas
        }

        [Fact]
        public async Task Resumir_DeveSomarOFiltroInteiroIgnorandoAPagina()
        {
            // O rodapé da tela não pode mudar ao virar de página — é a razão de o resumo existir.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            contexto.Abastecimentos.AddRange(
                NovoAbastecimento(c, dia, 100m),
                NovoAbastecimento(c, dia, 200m),
                NovoAbastecimento(c, dia, 300m));
            await contexto.SaveChangesAsync();

            var repositorio = new AbastecimentoRepository(contexto);
            var resumo = await repositorio.ResumirAsync(c.EmpresaId, Filtro(c, pagina: 2, tamanhoPagina: 1));

            Assert.Equal(3, resumo.Quantidade);
            Assert.Equal(600m, resumo.ValorTotal);
        }

        [Fact]
        public async Task Resumir_SemLancamentos_DeveVoltarZerado()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            var repositorio = new AbastecimentoRepository(contexto);
            var resumo = await repositorio.ResumirAsync(c.EmpresaId, Filtro(c));

            Assert.Equal(0, resumo.Quantidade);
            Assert.Equal(0m, resumo.ValorTotal);
        }

        [Fact]
        public async Task GetAnteriorPorOdometro_DevePegarOMaiorAbaixoEIgnorarOProprio()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            // Odômetros fora de ordem de data de propósito: a referência sai do odômetro,
            // não da data — é o que impede lançamento retroativo de virar km negativo.
            var a1 = NovoAbastecimento(c, dia.AddDays(-2), 100m); a1.Odometro = 1000;
            var a2 = NovoAbastecimento(c, dia, 200m); a2.Odometro = 3000;
            var a3 = NovoAbastecimento(c, dia.AddDays(-1), 300m); a3.Odometro = 2000;
            contexto.Abastecimentos.AddRange(a1, a2, a3);
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var repositorio = new AbastecimentoRepository(contexto);

            var anterior = await repositorio.GetAnteriorPorOdometroAsync(c.EmpresaId, a1.VeiculoId, 3000);
            Assert.Equal(2000, anterior!.Odometro);

            // Corrigindo o de 2000: ele sai da conta e a referência recua para 1000.
            var ignorando = await repositorio.GetAnteriorPorOdometroAsync(c.EmpresaId, a1.VeiculoId, 3000, a3.Id);
            Assert.Equal(1000, ignorando!.Odometro);

            // Primeiro do veículo: não há de onde partir.
            Assert.Null(await repositorio.GetAnteriorPorOdometroAsync(c.EmpresaId, a1.VeiculoId, 1000));
        }

        [Fact]
        public async Task Custos_DeveUnirAsDuasOrigensNumaListaSo()
        {
            // Unir duas tabelas diferentes numa lista só é o que sustenta a tela de custos
            // inteira. O EF não traduz `Concat` depois da projeção com constantes (foi o que
            // este teste provou), então a junção é feita em memória sobre páginas limitadas —
            // e é este teste que garante que o resultado continua o mesmo.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            contexto.Abastecimentos.Add(NovoAbastecimento(c, dia, 250m));
            contexto.Manutencoes.AddRange(
                NovaManutencao(c, StatusManutencao.Realizada, 110_000, custo: 1_200m, dataRealizacao: dia),
                // Fora do custo: concluída sem valor informado e pendente.
                NovaManutencao(c, StatusManutencao.Realizada, 115_000, custo: null, dataRealizacao: dia),
                NovaManutencao(c, StatusManutencao.Pendente, 130_000));
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var (itens, total) = await repositorio.ConsultarAsync(c.EmpresaId, new FiltroCusto(), 1, 25);

            Assert.Equal(2, total);
            Assert.Equal(1_450m, itens.Sum(l => l.Valor));
            Assert.Contains(itens, l => l.Origem == OrigemCusto.Abastecimento && l.Categoria == "Combustível");
            Assert.Contains(itens, l => l.Origem == OrigemCusto.Manutencao && l.Valor == 1_200m);
            // A navegação é lida dentro do Select, então a placa tem de vir preenchida.
            Assert.All(itens, l => Assert.False(string.IsNullOrWhiteSpace(l.VeiculoPlaca)));
        }

        [Fact]
        public async Task Custos_DevePaginarSemRepetirLinhaEntreOrigensNoMesmoDia()
        {
            // Ids de tabelas diferentes colidem. Sem o desempate por origem antes do id, a
            // ordem da junção é instável e a mesma linha aparece em duas páginas.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            for (var i = 0; i < 3; i++)
                contexto.Abastecimentos.Add(NovoAbastecimento(c, dia, 100m + i));
            for (var i = 0; i < 3; i++)
                contexto.Manutencoes.Add(NovaManutencao(c, StatusManutencao.Realizada,
                    110_000 + i, custo: 500m + i, dataRealizacao: dia));
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var (primeira, total) = await repositorio.ConsultarAsync(c.EmpresaId, new FiltroCusto(), 1, 3);
            var (segunda, _) = await repositorio.ConsultarAsync(c.EmpresaId, new FiltroCusto(), 2, 3);

            Assert.Equal(6, total);

            var chaves = primeira.Concat(segunda).Select(l => (l.Origem, l.OrigemId)).ToList();
            Assert.Equal(6, chaves.Distinct().Count());
        }

        [Fact]
        public async Task Custos_FiltradosPorMotorista_NaoDevemIncluirManutencao()
        {
            // Manutenção não é atribuída a motorista no modelo: o recorte por pessoa só pode
            // devolver abastecimento, e o total não pode carregar custo de oficina.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            contexto.Abastecimentos.Add(NovoAbastecimento(c, dia, 250m));
            contexto.Manutencoes.Add(NovaManutencao(c, StatusManutencao.Realizada, 110_000,
                custo: 1_200m, dataRealizacao: dia));
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var (itens, total) = await repositorio.ConsultarAsync(
                c.EmpresaId, new FiltroCusto(MotoristaId: c.MotoristaId), 1, 25);

            Assert.Equal(1, total);
            Assert.Equal(OrigemCusto.Abastecimento, itens.Single().Origem);

            // E o recorte impossível — manutenção de um motorista — devolve vazio, não erro.
            var (nenhum, zero) = await repositorio.ConsultarAsync(
                c.EmpresaId,
                new FiltroCusto(MotoristaId: c.MotoristaId, Origem: OrigemCusto.Manutencao), 1, 25);

            Assert.Empty(nenhum);
            Assert.Equal(0, zero);
        }

        [Fact]
        public async Task Custos_DeveRecortarPelaEmpresaEmTodasAsConsultas()
        {
            // A regra mais importante do sistema, provada no banco: nenhuma das cinco
            // consultas do read model pode enxergar a empresa vizinha.
            await using var contexto = fixture.CriarContexto();
            var minha = await MontarAsync(contexto);
            var outra = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            contexto.Abastecimentos.AddRange(
                NovoAbastecimento(minha, dia, 100m),
                NovoAbastecimento(outra, dia, 999m));
            contexto.Manutencoes.AddRange(
                NovaManutencao(minha, StatusManutencao.Realizada, 110_000, custo: 300m, dataRealizacao: dia),
                NovaManutencao(outra, StatusManutencao.Realizada, 110_000, custo: 888m, dataRealizacao: dia),
                NovaManutencao(outra, StatusManutencao.Realizada, 120_000, custo: null, dataRealizacao: dia));
            contexto.Rotas.AddRange(
                NovaRotaEncerrada(minha, dia, 1_000),
                NovaRotaEncerrada(outra, dia, 9_999));
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var filtro = new FiltroCusto();

            var (itens, total) = await repositorio.ConsultarAsync(minha.EmpresaId, filtro, 1, 25);
            Assert.Equal(2, total);
            Assert.Equal(400m, itens.Sum(l => l.Valor));

            var porVeiculo = await repositorio.SomarPorVeiculoAsync(minha.EmpresaId, filtro);
            Assert.Equal(400m, porVeiculo.Sum(v => v.Total));

            var porMes = await repositorio.SomarPorMesAsync(minha.EmpresaId, filtro);
            Assert.Equal(400m, porMes.Sum(m => m.Total));

            var km = await repositorio.SomarKmPorVeiculoAsync(minha.EmpresaId, filtro);
            Assert.Equal(1_000, km.Sum(k => k.Km));

            Assert.Equal(0, await repositorio.ContarManutencoesSemCustoAsync(minha.EmpresaId, filtro));
        }

        [Fact]
        public async Task Custos_DeveAgruparPorVeiculoEOrigemComOsDadosDoVeiculoPreenchidos()
        {
            // O GroupBy tem navegação na chave: se o EF deixar de traduzir, o nome e a placa
            // voltam vazios e a tabela do resumo fica sem identificar o veículo.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            contexto.Abastecimentos.AddRange(
                NovoAbastecimento(c, dia, 250m),
                NovoAbastecimento(c, dia, 150m));
            contexto.Manutencoes.Add(NovaManutencao(c, StatusManutencao.Realizada, 110_000,
                custo: 1_200m, dataRealizacao: dia));
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var totais = (await repositorio.SomarPorVeiculoAsync(c.EmpresaId, new FiltroCusto())).ToList();

            var abastecimento = Assert.Single(totais, t => t.Origem == OrigemCusto.Abastecimento);
            Assert.Equal(400m, abastecimento.Total);
            Assert.Equal(2, abastecimento.Quantidade);
            Assert.Equal("Scania R450", abastecimento.VeiculoNome);
            Assert.False(string.IsNullOrWhiteSpace(abastecimento.VeiculoPlaca));

            var manutencao = Assert.Single(totais, t => t.Origem == OrigemCusto.Manutencao);
            Assert.Equal(1_200m, manutencao.Total);
        }

        [Fact]
        public async Task Custos_DeveAgruparPorAnoEMesEContarManutencaoSemCusto()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            contexto.Abastecimentos.AddRange(
                NovoAbastecimento(c, new DateTime(2026, 7, 10), 100m),
                NovoAbastecimento(c, new DateTime(2026, 8, 10), 200m),
                NovoAbastecimento(c, new DateTime(2026, 8, 20), 300m));
            contexto.Manutencoes.AddRange(
                NovaManutencao(c, StatusManutencao.Realizada, 110_000, custo: null, dataRealizacao: new DateTime(2026, 8, 15)),
                NovaManutencao(c, StatusManutencao.Realizada, 115_000, custo: null, dataRealizacao: new DateTime(2026, 8, 16)));
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var filtro = new FiltroCusto();

            var meses = (await repositorio.SomarPorMesAsync(c.EmpresaId, filtro))
                .OrderBy(m => m.Mes).ToList();

            Assert.Equal(2, meses.Count);
            Assert.Equal((2026, 7, 100m), (meses[0].Ano, meses[0].Mes, meses[0].Total));
            Assert.Equal((2026, 8, 500m), (meses[1].Ano, meses[1].Mes, meses[1].Total));

            Assert.Equal(2, await repositorio.ContarManutencoesSemCustoAsync(c.EmpresaId, filtro));
        }

        [Fact]
        public async Task Custos_DeveSomarKmSoDasRotasEncerradas()
        {
            // Rota aberta não tem KmPercorrido: entra como zero e não pode virar denominador.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var dia = new DateTime(2026, 8, 30);

            var aberta = NovaRotaEncerrada(c, dia, 500);
            aberta.Ativo = true;
            aberta.DataFim = null;
            aberta.KmFinal = null;
            aberta.KmPercorrido = null;

            contexto.Rotas.AddRange(NovaRotaEncerrada(c, dia, 1_200), aberta);
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var km = await repositorio.SomarKmPorVeiculoAsync(c.EmpresaId, new FiltroCusto());

            var doVeiculo = Assert.Single(km);
            Assert.Equal(1_200, doVeiculo.Km);
            Assert.Equal(1, doVeiculo.Rotas);
            Assert.Equal("Scania R450", doVeiculo.VeiculoNome);
        }

        [Fact]
        public async Task Custos_DeveUnirAsTresOrigensNumaListaSo()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var tipoDespesa = await NovoTipoDespesaAsync(contexto, c);
            var dia = new DateTime(2026, 9, 1);

            contexto.Abastecimentos.Add(NovoAbastecimento(c, dia, 250m));
            contexto.Manutencoes.Add(NovaManutencao(c, StatusManutencao.Realizada, 110_000,
                custo: 1_200m, dataRealizacao: dia));
            contexto.Despesas.Add(NovaDespesa(c, tipoDespesa, dia, 100m));
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var (itens, total) = await repositorio.ConsultarAsync(c.EmpresaId, new FiltroCusto(), 1, 25);

            Assert.Equal(3, total);
            Assert.Equal(1_550m, itens.Sum(l => l.Valor));

            // A categoria da despesa vem do nome do tipo, via navegação lida dentro do Select.
            var despesa = Assert.Single(itens, l => l.Origem == OrigemCusto.Despesa);
            Assert.StartsWith("Pedagio", despesa.Categoria);
            Assert.False(string.IsNullOrWhiteSpace(despesa.VeiculoPlaca));
        }

        [Fact]
        public async Task Custos_FiltradosPorMotorista_DevemTrazerAbastecimentoEDespesaMasNaoManutencao()
        {
            // A diferença entre as três origens no recorte por pessoa: abastecimento sempre
            // tem dono, despesa tem quando é multa, e manutenção nunca tem.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var tipoDespesa = await NovoTipoDespesaAsync(contexto, c);
            var dia = new DateTime(2026, 9, 1);

            contexto.Abastecimentos.Add(NovoAbastecimento(c, dia, 250m));
            contexto.Manutencoes.Add(NovaManutencao(c, StatusManutencao.Realizada, 110_000,
                custo: 1_200m, dataRealizacao: dia));
            contexto.Despesas.AddRange(
                NovaDespesa(c, tipoDespesa, dia, 100m, motoristaId: c.MotoristaId),
                NovaDespesa(c, tipoDespesa, dia, 900m)); // sem dono (IPVA)
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var (itens, total) = await repositorio.ConsultarAsync(
                c.EmpresaId, new FiltroCusto(MotoristaId: c.MotoristaId), 1, 25);

            Assert.Equal(2, total);
            Assert.Equal(350m, itens.Sum(l => l.Valor));
            Assert.Contains(itens, l => l.Origem == OrigemCusto.Abastecimento);
            Assert.Contains(itens, l => l.Origem == OrigemCusto.Despesa);
            Assert.DoesNotContain(itens, l => l.Origem == OrigemCusto.Manutencao);
        }

        [Fact]
        public async Task Custos_DevePaginarSemRepetirLinhaEntreAsTresOrigens()
        {
            // Ids de três tabelas diferentes colidem entre si; o desempate por origem antes
            // do id é o que impede a mesma linha de aparecer em duas páginas.
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var tipoDespesa = await NovoTipoDespesaAsync(contexto, c);
            var dia = new DateTime(2026, 9, 1);

            for (var i = 0; i < 3; i++)
            {
                contexto.Abastecimentos.Add(NovoAbastecimento(c, dia, 100m + i));
                contexto.Manutencoes.Add(NovaManutencao(c, StatusManutencao.Realizada,
                    110_000 + i, custo: 500m + i, dataRealizacao: dia));
                contexto.Despesas.Add(NovaDespesa(c, tipoDespesa, dia, 50m + i));
            }
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var chaves = new List<(OrigemCusto, int)>();

            for (var pagina = 1; pagina <= 3; pagina++)
            {
                var (itens, _) = await repositorio.ConsultarAsync(c.EmpresaId, new FiltroCusto(), pagina, 3);
                chaves.AddRange(itens.Select(l => (l.Origem, l.OrigemId)));
            }

            Assert.Equal(9, chaves.Count);
            Assert.Equal(9, chaves.Distinct().Count());
        }

        [Fact]
        public async Task Custos_DeveSomarADespesaNosAgregadosERecortarPelaEmpresa()
        {
            await using var contexto = fixture.CriarContexto();
            var minha = await MontarAsync(contexto);
            var outra = await MontarAsync(contexto);
            var tipoMinha = await NovoTipoDespesaAsync(contexto, minha);
            var tipoOutra = await NovoTipoDespesaAsync(contexto, outra);
            var dia = new DateTime(2026, 9, 1);

            contexto.Despesas.AddRange(
                NovaDespesa(minha, tipoMinha, dia, 300m),
                NovaDespesa(outra, tipoOutra, dia, 999m));
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var filtro = new FiltroCusto();

            var porVeiculo = await repositorio.SomarPorVeiculoAsync(minha.EmpresaId, filtro);
            var linha = Assert.Single(porVeiculo);
            Assert.Equal(OrigemCusto.Despesa, linha.Origem);
            Assert.Equal(300m, linha.Total);
            Assert.False(string.IsNullOrWhiteSpace(linha.VeiculoPlaca));

            var porMes = await repositorio.SomarPorMesAsync(minha.EmpresaId, filtro);
            var mes = Assert.Single(porMes);
            Assert.Equal((2026, 9, 300m), (mes.Ano, mes.Mes, mes.Total));
        }

        [Fact]
        public async Task Custos_FiltradosPorOrigemDespesa_DevemDeixarDeForaAsOutrasDuas()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var tipoDespesa = await NovoTipoDespesaAsync(contexto, c);
            var dia = new DateTime(2026, 9, 1);

            contexto.Abastecimentos.Add(NovoAbastecimento(c, dia, 250m));
            contexto.Manutencoes.Add(NovaManutencao(c, StatusManutencao.Realizada, 110_000,
                custo: 1_200m, dataRealizacao: dia));
            contexto.Despesas.Add(NovaDespesa(c, tipoDespesa, dia, 100m));
            await contexto.SaveChangesAsync();

            var repositorio = new CustoRepository(contexto);
            var (itens, total) = await repositorio.ConsultarAsync(
                c.EmpresaId, new FiltroCusto(Origem: OrigemCusto.Despesa), 1, 25);

            Assert.Equal(1, total);
            Assert.Equal(100m, Assert.Single(itens).Valor);
        }

        private static Abastecimento NovoAbastecimento(Cenario c, DateTime data, decimal valor,
            int odometro = 100_000, decimal litros = 50m) => new()
        {
            EmpresaId = c.EmpresaId,
            VeiculoId = c.VeiculoId,
            MotoristaId = c.MotoristaId,
            UsuarioId = c.MotoristaId,
            TipoCombustivelId = c.TipoCombustivelId,
            PostoId = c.PostoId,
            // Os defaults servem aos testes de custo, que só olham o valor; os de consumo
            // passam odômetro e litros porque é deles que a métrica sai.
            Litros = litros,
            ValorLitro = 6.400m,
            Valor = valor,
            Odometro = odometro,
            NotaFiscal = "123456",
            DataAbastecimento = data,
            DataInclusao = DateTime.Now
        };

        private static Manutencao NovaManutencao(Cenario c, StatusManutencao status, int kmPrevista,
            decimal? custo = null, DateTime? dataRealizacao = null) => new()
        {
            EmpresaId = c.EmpresaId,
            VeiculoId = c.VeiculoId,
            TipoManutencaoId = c.TipoId,
            QuilometragemPrevista = kmPrevista,
            Status = status,
            Custo = custo,
            DataRealizacao = dataRealizacao,
            DataInclusao = DateTime.Now
        };

        /// <summary>
        /// Cria o tipo de despesa da empresa do cenário e devolve o id. O catálogo não é
        /// semeado por <c>MontarAsync</c> porque só a despesa precisa dele.
        /// </summary>
        private static async Task<int> NovoTipoDespesaAsync(Frota360DbContext contexto, Cenario c)
        {
            var tipo = new TipoDespesa
            {
                EmpresaId = c.EmpresaId,
                Nome = Unicos.Texto("Pedagio"),
                Ativo = true,
                DataInclusao = DateTime.Now
            };
            contexto.TiposDespesa.Add(tipo);
            await contexto.SaveChangesAsync();
            return tipo.Id;
        }

        private static Despesa NovaDespesa(Cenario c, int tipoDespesaId, DateTime data, decimal valor,
            int? motoristaId = null) => new()
        {
            EmpresaId = c.EmpresaId,
            VeiculoId = c.VeiculoId,
            TipoDespesaId = tipoDespesaId,
            MotoristaId = motoristaId,
            Valor = valor,
            DataDespesa = data,
            DataInclusao = DateTime.Now
        };

        private static Rota NovaRotaEncerrada(Cenario c, DateTime dataFim, int kmPercorrido) => new()
        {
            EmpresaId = c.EmpresaId,
            Origem = "Curitiba",
            Destino = "Sao Paulo",
            CodigoMotorista = c.MotoristaId,
            CodigoVeiculo = c.VeiculoId,
            Ativo = false,
            DataInicio = dataFim.AddDays(-1),
            DataFim = dataFim,
            KmInicial = 100_000,
            KmFinal = 100_000 + kmPercorrido,
            KmPercorrido = kmPercorrido,
            DataInclusao = DateTime.Now
        };
    }
}
