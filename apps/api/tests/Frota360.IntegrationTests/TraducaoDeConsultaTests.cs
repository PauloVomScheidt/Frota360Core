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
        private sealed record Cenario(int EmpresaId, int VeiculoId, int MotoristaId, int TipoId);

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
            contexto.Veiculos.Add(veiculo);
            contexto.Usuarios.Add(motorista);
            contexto.TiposManutencao.Add(tipo);
            await contexto.SaveChangesAsync();

            return new Cenario(empresa.Id, veiculo.Id, motorista.Id, tipo.Id);
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
            var noDia = await repositorio.GetAllAsync(c.EmpresaId, de: dia, ate: dia);

            var valores = noDia.Select(a => a.Valor).ToList();
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
            var intervalo = await repositorio.GetAllAsync(c.EmpresaId, de: dia.AddDays(-1), ate: dia.AddDays(1));

            Assert.Equal(3, intervalo.Count());
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
            var filtradas = await repositorio.GetAllAsync(c.EmpresaId, status: StatusManutencao.Realizada);
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
            var ordenadas = (await repositorio.GetAllAsync(c.EmpresaId)).ToList();

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
            var meus = await repositorio.GetAllAsync(minha.EmpresaId);

            Assert.Single(meus);
            Assert.Equal(100m, meus.Single().Valor);
        }

        private static Abastecimento NovoAbastecimento(Cenario c, DateTime data, decimal valor) => new()
        {
            EmpresaId = c.EmpresaId,
            VeiculoId = c.VeiculoId,
            MotoristaId = c.MotoristaId,
            UsuarioId = c.MotoristaId,
            Valor = valor,
            DataAbastecimento = data,
            DataInclusao = DateTime.Now
        };

        private static Manutencao NovaManutencao(Cenario c, StatusManutencao status, int kmPrevista) => new()
        {
            EmpresaId = c.EmpresaId,
            VeiculoId = c.VeiculoId,
            TipoManutencaoId = c.TipoId,
            QuilometragemPrevista = kmPrevista,
            Status = status,
            DataInclusao = DateTime.Now
        };
    }
}
