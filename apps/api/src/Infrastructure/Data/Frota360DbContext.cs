using Frota360.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Data
{
    public class Frota360DbContext(DbContextOptions<Frota360DbContext> options) : DbContext(options)
    {
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Convite> Convites { get; set; }
        public DbSet<Veiculo> Veiculos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rota> Rotas { get; set; }
        public DbSet<TipoManutencao> TiposManutencao { get; set; }
        public DbSet<Manutencao> Manutencoes { get; set; }
        public DbSet<Abastecimento> Abastecimentos { get; set; }
        public DbSet<LogAuditoria> LogsAuditoria { get; set; }

        /// <summary>
        /// Toda data persistida é gravada sem fuso, guardando o relógio de parede de Brasília.
        /// O <see cref="DataSemFusoConverter"/> explica por que o Kind precisa ser normalizado
        /// em vez de simplesmente escolhermos um tipo de coluna. Vale para <c>DateTime</c> e
        /// <c>DateTime?</c> de todas as entidades — não há exceção no modelo.
        /// </summary>
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>()
                .HaveColumnType("timestamp without time zone")
                .HaveConversion<DataSemFusoConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Empresa>(entity =>
            {
                entity.ToTable("Empresa");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).HasMaxLength(150).IsRequired();
                entity.Property(e => e.CNPJ).HasMaxLength(14);
                entity.HasIndex(e => e.CNPJ).IsUnique().HasFilter("\"CNPJ\" IS NOT NULL");
                entity.Property(e => e.Ativo).HasDefaultValue(true);
                entity.Property(e => e.DataInclusao).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'");
            });

            modelBuilder.Entity<Convite>(entity =>
            {
                entity.ToTable("Convite");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Email).HasMaxLength(150).IsRequired();
                entity.Property(c => c.Role).HasMaxLength(20).IsRequired();
                entity.Property(c => c.TokenHash).HasMaxLength(100).IsRequired();
                entity.HasIndex(c => c.TokenHash).IsUnique();
                entity.Property(c => c.DataInclusao).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'");

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(c => c.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Usuario>()
                      .WithMany()
                      .HasForeignKey(c => c.CriadoPorUsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Veiculo>(entity =>
            {
                entity.ToTable("Veiculo");
                entity.HasKey(v => v.Id);

                entity.Property(v => v.NomeVeiculo).HasMaxLength(100).IsRequired();
                entity.Property(v => v.MarcaVeiculo).HasMaxLength(100).IsRequired();
                entity.Property(v => v.Placa).HasMaxLength(10).IsRequired();
                entity.Property(v => v.Quilometragem).HasDefaultValue(0);
                entity.Property(v => v.UltimoMotorista).HasMaxLength(100);
                entity.Property(v => v.DataInclusao).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'");

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(v => v.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Nome).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
                entity.HasIndex(u => u.Email).IsUnique(); 
                entity.Property(u => u.SenhaHash).IsRequired();
                entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
                entity.Property(u => u.Ativo).HasDefaultValue(true);
                entity.Property(u => u.RefreshTokenHash).HasMaxLength(100);
                entity.HasIndex(u => u.RefreshTokenHash);
                entity.Property(u => u.ResetSenhaTokenHash).HasMaxLength(100);
                entity.HasIndex(u => u.ResetSenhaTokenHash);
                entity.Property(u => u.DataInclusao).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'");

                // Opcionais: quem não informou fica com nulo, e o índice filtrado deixa
                // esses de fora — só barra dois CPFs iguais na mesma empresa.
                entity.Property(u => u.CPF).HasMaxLength(11);
                entity.HasIndex(u => new { u.EmpresaId, u.CPF })
                      .IsUnique()
                      .HasFilter("\"CPF\" IS NOT NULL");

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(u => u.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Rota>(entity =>
            {
                entity.ToTable("Rota");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Origem).HasMaxLength(100).IsRequired();
                entity.Property(r => r.Destino).HasMaxLength(150).IsRequired();
                entity.Property(r => r.Ativo).HasDefaultValue(true);
                entity.Property(r => r.DataInclusao).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'");

                // "Quais veiculos estao rodando" roda em toda listagem de veiculo e no
                // dashboard. Substitui o indice de FK (EmpresaId) que o EF criava sozinho:
                // este o cobre como prefixo.
                entity.HasIndex(r => new { r.EmpresaId, r.Ativo, r.CodigoVeiculo });

                // O motorista é um Usuario. Restrict porque usuário nunca é excluído,
                // só desativado — o histórico de rotas fica inapagável por acidente.
                entity.HasOne(r => r.Motorista)
                      .WithMany()
                      .HasForeignKey(r => r.CodigoMotorista)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Veiculo)
                      .WithMany()
                      .HasForeignKey(r => r.CodigoVeiculo);

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(r => r.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TipoManutencao>(entity =>
            {
                entity.ToTable("TipoManutencao");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Nome).HasMaxLength(100).IsRequired();
                entity.Property(t => t.Ativo).HasDefaultValue(true);
                entity.Property(t => t.DataInclusao).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'");

                // Nome unico por empresa: cada transportadora nomeia seus tipos como quiser
                entity.HasIndex(t => new { t.EmpresaId, t.Nome }).IsUnique();

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(t => t.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Manutencao>(entity =>
            {
                entity.ToTable("Manutencao");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Observacao).HasMaxLength(500);
                entity.Property(m => m.Custo).HasPrecision(10, 2);
                entity.Property(m => m.DataInclusao).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'");

                // Persistido como texto: o banco fica legivel e novos status nao dependem da ordem do enum
                entity.Property(m => m.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20)
                      .IsRequired();

                // Consulta dominante da tela: pendencias de um veiculo
                entity.HasIndex(m => new { m.EmpresaId, m.VeiculoId, m.Status });

                entity.HasOne(m => m.Veiculo)
                      .WithMany()
                      .HasForeignKey(m => m.VeiculoId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Tipo)
                      .WithMany()
                      .HasForeignKey(m => m.TipoManutencaoId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(m => m.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Abastecimento>(entity =>
            {
                entity.ToTable("Abastecimento");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Valor).HasPrecision(10, 2);
                entity.Property(a => a.Observacao).HasMaxLength(500);
                entity.Property(a => a.DataInclusao).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'");

                // Consulta dominante da tela: historico de um veiculo em ordem de data.
                entity.HasIndex(a => new { a.EmpresaId, a.VeiculoId, a.DataAbastecimento });

                // Recorte do motorista e o relatorio de gasto por motorista.
                entity.HasIndex(a => new { a.EmpresaId, a.MotoristaId, a.DataAbastecimento });

                entity.HasOne(a => a.Veiculo)
                      .WithMany()
                      .HasForeignKey(a => a.VeiculoId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Excluir a rota nao pode levar junto o abastecimento: o gasto aconteceu de
                // verdade, e o vinculo com a viagem e so contexto.
                entity.HasOne(a => a.Rota)
                      .WithMany()
                      .HasForeignKey(a => a.RotaId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Duas FKs para Usuario na mesma tabela: de quem e o gasto e quem digitou.
                // Ambas Restrict, entao nao ha ciclo de cascata.
                entity.HasOne(a => a.Motorista)
                      .WithMany()
                      .HasForeignKey(a => a.MotoristaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Usuario)
                      .WithMany()
                      .HasForeignKey(a => a.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(a => a.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LogAuditoria>(entity =>
            {
                entity.ToTable("LogAuditoria");
                entity.HasKey(l => l.Id);

                entity.Property(l => l.UsuarioNome).HasMaxLength(100).IsRequired();
                entity.Property(l => l.UsuarioEmail).HasMaxLength(150).IsRequired();
                entity.Property(l => l.UsuarioRole).HasMaxLength(20).IsRequired();
                entity.Property(l => l.Entidade).HasMaxLength(40).IsRequired();
                entity.Property(l => l.Acao).HasMaxLength(30).IsRequired();
                entity.Property(l => l.Descricao).HasMaxLength(300).IsRequired();
                entity.Property(l => l.IpOrigem).HasMaxLength(45); // comporta IPv6
                entity.Property(l => l.DataHora).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'");

                // Um índice por consulta real da tela: a listagem, o histórico de um
                // registro e o histórico de uma pessoa.
                entity.HasIndex(l => new { l.EmpresaId, l.DataHora });
                entity.HasIndex(l => new { l.EmpresaId, l.Entidade, l.EntidadeId });
                entity.HasIndex(l => new { l.EmpresaId, l.UsuarioId, l.DataHora });

                // Restrict nos dois: a trilha é histórico e não pode ser apagada em cascata
                // por uma operação sobre a empresa ou o usuário.
                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(l => l.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Usuario>()
                      .WithMany()
                      .HasForeignKey(l => l.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
