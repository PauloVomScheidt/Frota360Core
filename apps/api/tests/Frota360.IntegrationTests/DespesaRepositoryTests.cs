using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Infrastructure.Data;
using Frota360.Infrastructure.Repositories;

namespace Frota360.IntegrationTests
{
    /// <summary>
    /// A despesa é a única origem de custo cuja tabela é fonte de verdade. O que só o banco
    /// prova: a página e o resumo enxergam o mesmo recorte, o "até" inclui o dia inteiro e a
    /// ordenação é total — sem isso a página 2 repete linha da 1.
    /// </summary>
    [Collection(BancoCollection.Nome)]
    public class DespesaRepositoryTests(BancoFixture fixture)
    {
        private sealed record Cenario(int EmpresaId, int VeiculoId, int OutroVeiculoId, int MotoristaId,
                                      int TipoId, int OutroTipoId);

        private static async Task<Cenario> MontarAsync(Frota360DbContext contexto)
        {
            var empresa = new Empresa { Nome = Unicos.Texto("Empresa"), DataInclusao = DateTime.Now };
            contexto.Empresas.Add(empresa);
            await contexto.SaveChangesAsync();

            Veiculo NovoVeiculo() => new()
            {
                EmpresaId = empresa.Id,
                NomeVeiculo = "Volvo FH",
                MarcaVeiculo = "Volvo",
                Placa = Unicos.Placa(),
                Quilometragem = 50_000,
                DataInclusao = DateTime.Now
            };
            TipoDespesa NovoTipo(string nome) => new()
            {
                EmpresaId = empresa.Id,
                Nome = Unicos.Texto(nome),
                DataInclusao = DateTime.Now
            };

            var veiculo = NovoVeiculo();
            var outroVeiculo = NovoVeiculo();
            var motorista = new Usuario
            {
                EmpresaId = empresa.Id,
                Nome = "Motorista",
                Email = Unicos.Email("desp"),
                SenhaHash = "$2a$11$abcdefghijklmnopqrstuv",
                Role = Roles.Motorista,
                DataInclusao = DateTime.Now
            };
            var pedagio = NovoTipo("Pedagio");
            var multa = NovoTipo("Multa");

            contexto.AddRange(veiculo, outroVeiculo, motorista, pedagio, multa);
            await contexto.SaveChangesAsync();

            return new Cenario(empresa.Id, veiculo.Id, outroVeiculo.Id, motorista.Id, pedagio.Id, multa.Id);
        }

        private static Despesa NovaDespesa(Cenario c, DateTime data, decimal valor,
            int? veiculoId = null, int? tipoId = null, int? motoristaId = null) => new()
        {
            EmpresaId = c.EmpresaId,
            VeiculoId = veiculoId ?? c.VeiculoId,
            TipoDespesaId = tipoId ?? c.TipoId,
            MotoristaId = motoristaId,
            Valor = valor,
            DataDespesa = data,
            DataInclusao = DateTime.Now
        };

        private static FiltroDespesa Filtro(int pagina = 1, int tamanhoPagina = 50, int? veiculoId = null,
            int? motoristaId = null, int? tipoId = null, DateTime? de = null, DateTime? ate = null)
            => new(pagina, tamanhoPagina, veiculoId, motoristaId, tipoId, de, ate);

        [Fact]
        public async Task Crud_DeveGravarLerComNavegacoesCorrigirEExcluir()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var repositorio = new DespesaRepository(contexto);

            var criada = await repositorio.AddAsync(NovaDespesa(c, new DateTime(2026, 9, 1), 25.40m, motoristaId: c.MotoristaId));
            contexto.ChangeTracker.Clear();

            var lida = await repositorio.GetByIdAsync(criada.Id, c.EmpresaId);
            Assert.NotNull(lida);
            Assert.NotNull(lida.Veiculo);
            Assert.NotNull(lida.Tipo);
            Assert.Equal("Motorista", lida.Motorista?.Nome);

            lida.Valor = 30m;
            lida.Observacao = "Corrigido";
            await repositorio.UpdateAsync(lida);
            contexto.ChangeTracker.Clear();

            var corrigida = await repositorio.GetByIdAsync(criada.Id, c.EmpresaId);
            Assert.Equal(30m, corrigida!.Valor);
            Assert.Equal("Corrigido", corrigida.Observacao);

            await repositorio.DeleteAsync(corrigida);
            contexto.ChangeTracker.Clear();

            Assert.Null(await repositorio.GetByIdAsync(criada.Id, c.EmpresaId));
        }

        [Fact]
        public async Task GetById_DeOutraEmpresa_DeveVoltarNulo()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var outra = await MontarAsync(contexto);
            var repositorio = new DespesaRepository(contexto);

            var despesa = await repositorio.AddAsync(NovaDespesa(c, new DateTime(2026, 9, 1), 10m));

            Assert.Null(await repositorio.GetByIdAsync(despesa.Id, outra.EmpresaId));
        }

        [Fact]
        public async Task Consultar_DeveFiltrarPorVeiculoMotoristaTipoEPeriodoComOAteInclusivo()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var outra = await MontarAsync(contexto);

            contexto.Despesas.AddRange(
                NovaDespesa(c, new DateTime(2026, 8, 10, 8, 0, 0), 10m),
                NovaDespesa(c, new DateTime(2026, 8, 11, 23, 30, 0), 20m, motoristaId: c.MotoristaId),
                NovaDespesa(c, new DateTime(2026, 8, 12), 40m, veiculoId: c.OutroVeiculoId),
                NovaDespesa(c, new DateTime(2026, 8, 11), 80m, tipoId: c.OutroTipoId),
                NovaDespesa(outra, new DateTime(2026, 8, 11), 1_000m));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var repositorio = new DespesaRepository(contexto);

            async Task<decimal[]> Valores(FiltroDespesa filtro) =>
                (await repositorio.ConsultarAsync(c.EmpresaId, filtro)).Itens.Select(d => d.Valor).OrderBy(v => v).ToArray();

            Assert.Equal([10m, 20m, 40m, 80m], await Valores(Filtro()));
            Assert.Equal([40m], await Valores(Filtro(veiculoId: c.OutroVeiculoId)));
            Assert.Equal([20m], await Valores(Filtro(motoristaId: c.MotoristaId)));
            Assert.Equal([80m], await Valores(Filtro(tipoId: c.OutroTipoId)));
            Assert.Equal([20m, 80m], await Valores(Filtro(de: new DateTime(2026, 8, 11), ate: new DateTime(2026, 8, 11))));
        }

        [Fact]
        public async Task Consultar_ComMesmaData_DevePaginarSemRepetirLinha()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            var mesmaData = new DateTime(2026, 9, 5);
            contexto.Despesas.AddRange(Enumerable.Range(1, 5).Select(i => NovaDespesa(c, mesmaData, i)));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var repositorio = new DespesaRepository(contexto);
            var pagina1 = await repositorio.ConsultarAsync(c.EmpresaId, Filtro(pagina: 1, tamanhoPagina: 3));
            var pagina2 = await repositorio.ConsultarAsync(c.EmpresaId, Filtro(pagina: 2, tamanhoPagina: 3));

            var ids = pagina1.Itens.Concat(pagina2.Itens).Select(d => d.Id).ToList();
            Assert.Equal(5, pagina1.Total);
            Assert.Equal(5, ids.Distinct().Count());
            Assert.Equal(ids.OrderByDescending(id => id), ids);
        }

        [Fact]
        public async Task Resumir_DeveSomarOMesmoRecorteDaConsulta()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            contexto.Despesas.AddRange(
                NovaDespesa(c, new DateTime(2026, 8, 10), 10.25m),
                NovaDespesa(c, new DateTime(2026, 8, 11), 20.50m),
                NovaDespesa(c, new DateTime(2026, 8, 11), 99m, veiculoId: c.OutroVeiculoId));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var repositorio = new DespesaRepository(contexto);
            var filtro = Filtro(veiculoId: c.VeiculoId);

            var resumo = await repositorio.ResumirAsync(c.EmpresaId, filtro);
            var (itens, total) = await repositorio.ConsultarAsync(c.EmpresaId, filtro);

            Assert.Equal(new ResumoLancamentos(2, 30.75m), resumo);
            Assert.Equal(total, resumo.Quantidade);
            Assert.Equal(itens.Sum(d => d.Valor), resumo.ValorTotal);
        }

        [Fact]
        public async Task Resumir_SemLancamentos_DeveVoltarZerado()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);

            var resumo = await new DespesaRepository(contexto).ResumirAsync(c.EmpresaId, Filtro());

            Assert.Equal(new ResumoLancamentos(0, 0m), resumo);
        }

        [Fact]
        public async Task Existe_DeveEnxergarVeiculoETipoEmUsoSoNaPropriaEmpresa()
        {
            await using var contexto = fixture.CriarContexto();
            var c = await MontarAsync(contexto);
            var outra = await MontarAsync(contexto);
            var repositorio = new DespesaRepository(contexto);

            await repositorio.AddAsync(NovaDespesa(c, new DateTime(2026, 9, 1), 10m));

            Assert.True(await repositorio.ExisteComVeiculoAsync(c.EmpresaId, c.VeiculoId));
            Assert.False(await repositorio.ExisteComVeiculoAsync(c.EmpresaId, c.OutroVeiculoId));
            Assert.False(await repositorio.ExisteComVeiculoAsync(outra.EmpresaId, c.VeiculoId));
            Assert.True(await repositorio.ExisteComTipoAsync(c.EmpresaId, c.TipoId));
            Assert.False(await repositorio.ExisteComTipoAsync(c.EmpresaId, c.OutroTipoId));
        }
    }
}
