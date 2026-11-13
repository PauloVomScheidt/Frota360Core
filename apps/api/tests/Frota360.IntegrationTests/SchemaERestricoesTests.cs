using Frota360.Domain.Entities;
using Frota360.Infrastructure.Data;
using Frota360.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Frota360.IntegrationTests
{
    /// <summary>
    /// As garantias que moram no banco, não no C#: índices, restrições e precisão. Nenhuma
    /// delas é observável pelos testes unitários, que mockam repositório.
    /// </summary>
    [Collection(BancoCollection.Nome)]
    public class SchemaERestricoesTests(BancoFixture fixture)
    {
        private async Task<Empresa> NovaEmpresaAsync(Frota360DbContext contexto)
        {
            var empresa = new Empresa { Nome = Unicos.Texto("Empresa"), DataInclusao = DateTime.Now };
            contexto.Empresas.Add(empresa);
            await contexto.SaveChangesAsync();
            return empresa;
        }

        private static Usuario NovoUsuario(int empresaId, string email, string? cpf = null) => new()
        {
            EmpresaId = empresaId,
            Nome = "Fulano",
            Email = email,
            SenhaHash = "$2a$11$abcdefghijklmnopqrstuv",
            Role = Domain.Common.Roles.Operador,
            CPF = cpf,
            DataInclusao = DateTime.Now
        };

        [Fact]
        public async Task Migrations_DevemTerCriadoTodasAsTabelas()
        {
            await using var contexto = fixture.CriarContexto();

            // O EF projeta SqlQuery<T> sobre uma coluna chamada "Value" — daí o alias.
            var tabelas = await contexto.Database
                .SqlQuery<string>($@"SELECT table_name AS ""Value"" FROM information_schema.tables
                                     WHERE table_schema = 'public'")
                .ToListAsync();

            Assert.Contains("Empresa", tabelas);
            Assert.Contains("Usuario", tabelas);
            Assert.Contains("Veiculo", tabelas);
            Assert.Contains("Rota", tabelas);
            Assert.Contains("Manutencao", tabelas);
            Assert.Contains("TipoManutencao", tabelas);
            Assert.Contains("Abastecimento", tabelas);
            Assert.Contains("TipoDespesa", tabelas);
            Assert.Contains("TipoCombustivel", tabelas);
            Assert.Contains("Posto", tabelas);
            Assert.Contains("Despesa", tabelas);
            Assert.Contains("Convite", tabelas);
            Assert.Contains("LogAuditoria", tabelas);
        }

        [Fact]
        public async Task GetByEmail_DeveIgnorarCaixa()
        {
            // No SQL Server a collation case-insensitive dava isto de graça. No PostgreSQL a
            // garantia vem de EmailNormalizado; sem ela, quem digitar outra caixa não loga.
            await using var contexto = fixture.CriarContexto();
            var empresa = await NovaEmpresaAsync(contexto);
            var email = Unicos.Email("Fulano.Teste").ToUpperInvariant();

            contexto.Usuarios.Add(NovoUsuario(empresa.Id, Domain.Common.EmailNormalizado.De(email)));
            await contexto.SaveChangesAsync();

            var repositorio = new UsuarioRepository(contexto);

            Assert.NotNull(await repositorio.GetByEmailAsync(email.ToUpperInvariant()));
            Assert.NotNull(await repositorio.GetByEmailAsync(email.ToLowerInvariant()));
            Assert.NotNull(await repositorio.GetByEmailAsync($"  {email}  "));
            Assert.True(await repositorio.ExisteEmailAsync(email.ToUpperInvariant()));
        }

        [Fact]
        public async Task IndiceFiltradoDeCpf_DevePermitirVariosNulos()
        {
            // O filtro "CPF" IS NOT NULL existe para isto: CPF é opcional, e quem não informou
            // não pode colidir com os outros que também não informaram.
            await using var contexto = fixture.CriarContexto();
            var empresa = await NovaEmpresaAsync(contexto);

            contexto.Usuarios.AddRange(
                NovoUsuario(empresa.Id, Unicos.Email("semcpf")),
                NovoUsuario(empresa.Id, Unicos.Email("semcpf")),
                NovoUsuario(empresa.Id, Unicos.Email("semcpf")));

            await contexto.SaveChangesAsync();

            var semCpf = await contexto.Usuarios.CountAsync(u => u.EmpresaId == empresa.Id && u.CPF == null);
            Assert.Equal(3, semCpf);
        }

        [Fact]
        public async Task IndiceFiltradoDeCpf_DeveBarrarDuplicataNaMesmaEmpresa()
        {
            await using var contexto = fixture.CriarContexto();
            var empresa = await NovaEmpresaAsync(contexto);
            const string cpf = "11144477735";

            contexto.Usuarios.Add(NovoUsuario(empresa.Id, Unicos.Email("cpf"), cpf));
            await contexto.SaveChangesAsync();

            contexto.Usuarios.Add(NovoUsuario(empresa.Id, Unicos.Email("cpf"), cpf));

            await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
        }

        [Fact]
        public async Task IndiceFiltradoDeCpf_DevePermitirOMesmoCpfEmEmpresasDiferentes()
        {
            // O índice é composto com EmpresaId: a mesma pessoa pode existir em duas
            // transportadoras. É a regra multi-tenant materializada no schema.
            await using var contexto = fixture.CriarContexto();
            var primeira = await NovaEmpresaAsync(contexto);
            var segunda = await NovaEmpresaAsync(contexto);
            const string cpf = "52998224725";

            contexto.Usuarios.Add(NovoUsuario(primeira.Id, Unicos.Email("multi"), cpf));
            contexto.Usuarios.Add(NovoUsuario(segunda.Id, Unicos.Email("multi"), cpf));

            await contexto.SaveChangesAsync();

            Assert.Equal(2, await contexto.Usuarios.CountAsync(u => u.CPF == cpf));
        }

        [Fact]
        public async Task EmailUnico_DeveSerGlobalENaoPorEmpresa()
        {
            // Exceção deliberada ao multi-tenant: o e-mail identifica a pessoa no login, que
            // acontece antes de haver empresa no contexto.
            await using var contexto = fixture.CriarContexto();
            var primeira = await NovaEmpresaAsync(contexto);
            var segunda = await NovaEmpresaAsync(contexto);
            var email = Unicos.Email("global");

            contexto.Usuarios.Add(NovoUsuario(primeira.Id, email));
            await contexto.SaveChangesAsync();

            contexto.Usuarios.Add(NovoUsuario(segunda.Id, email));

            await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
        }

        [Fact]
        public async Task Decimal_DevePreservarDuasCasas()
        {
            // numeric(10,2): valor em reais não pode perder centavo no round-trip.
            await using var contexto = fixture.CriarContexto();
            var empresa = await NovaEmpresaAsync(contexto);

            var veiculo = new Veiculo
            {
                EmpresaId = empresa.Id,
                NomeVeiculo = "Scania R450",
                MarcaVeiculo = "Scania",
                Placa = Unicos.Placa(),
                Quilometragem = 100_000,
                DataInclusao = DateTime.Now
            };
            var motorista = NovoUsuario(empresa.Id, Unicos.Email("mot"));
            contexto.Veiculos.Add(veiculo);
            contexto.Usuarios.Add(motorista);
            await contexto.SaveChangesAsync();

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
            contexto.TiposCombustivel.Add(combustivel);
            contexto.Postos.Add(posto);
            await contexto.SaveChangesAsync();

            contexto.Abastecimentos.Add(new Abastecimento
            {
                EmpresaId = empresa.Id,
                VeiculoId = veiculo.Id,
                MotoristaId = motorista.Id,
                UsuarioId = motorista.Id,
                TipoCombustivelId = combustivel.Id,
                PostoId = posto.Id,
                // Três casas de cada lado: é o caso que numeric(9,3)/numeric(8,3) precisa
                // aguentar sem arredondar na ida ao banco.
                Litros = 48.567m,
                ValorLitro = 6.199m,
                Valor = 1234.56m,
                Odometro = 152_340,
                NotaFiscal = "NF-000123456",
                Frentista = "Carlos",
                DataAbastecimento = new DateTime(2026, 8, 30),
                DataInclusao = DateTime.Now
            });
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var lido = await contexto.Abastecimentos.AsNoTracking()
                .SingleAsync(a => a.EmpresaId == empresa.Id);

            Assert.Equal(1234.56m, lido.Valor);
            Assert.Equal(48.567m, lido.Litros);
            Assert.Equal(6.199m, lido.ValorLitro);
            Assert.Equal(152_340, lido.Odometro);
            Assert.Equal("NF-000123456", lido.NotaFiscal);
            Assert.Equal("Carlos", lido.Frentista);
        }
    }
}
