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
        public DbSet<Motorista> Motoristas { get; set; }
        public DbSet<Rota> Rotas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Empresa>(entity =>
            {
                entity.ToTable("Empresa");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).HasMaxLength(150).IsRequired();
                entity.Property(e => e.CNPJ).HasMaxLength(14);
                entity.HasIndex(e => e.CNPJ).IsUnique().HasFilter("[CNPJ] IS NOT NULL");
                entity.Property(e => e.Ativo).HasDefaultValue(true);
                entity.Property(e => e.DataInclusao).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Convite>(entity =>
            {
                entity.ToTable("Convite");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Email).HasMaxLength(150).IsRequired();
                entity.Property(c => c.Role).HasMaxLength(20).IsRequired();
                entity.Property(c => c.TokenHash).HasMaxLength(100).IsRequired();
                entity.HasIndex(c => c.TokenHash).IsUnique();
                entity.Property(c => c.DataInclusao).HasDefaultValueSql("GETDATE()");

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
                entity.Property(v => v.DataInclusao).HasDefaultValueSql("GETDATE()");

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
                entity.Property(u => u.DataInclusao).HasDefaultValueSql("GETDATE()");

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(u => u.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Motorista>(entity =>
            {
                entity.ToTable("Motorista");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Nome).HasMaxLength(100).IsRequired();
                entity.Property(m => m.Email).HasMaxLength(150).IsRequired();
                entity.Property(m => m.CPF).HasMaxLength(11).IsRequired();
                entity.Property(m => m.DataInclusao).HasDefaultValueSql("GETDATE()");

                // Unicidade por empresa: transportadoras diferentes podem cadastrar o mesmo motorista
                entity.HasIndex(m => new { m.EmpresaId, m.Email }).IsUnique();
                entity.HasIndex(m => new { m.EmpresaId, m.CPF }).IsUnique();

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(m => m.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Rota>(entity =>
            {
                entity.ToTable("Rota");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Origem).HasMaxLength(100).IsRequired();
                entity.Property(r => r.Destino).HasMaxLength(150).IsRequired();
                entity.Property(r => r.Ativo).HasDefaultValue(true);
                entity.Property(r => r.DataInclusao).HasDefaultValueSql("GETDATE()");

                entity.HasOne(r => r.Motorista)
                      .WithMany()
                      .HasForeignKey(r => r.CodigoMotorista);

                entity.HasOne(r => r.Veiculo)
                      .WithMany()
                      .HasForeignKey(r => r.CodigoVeiculo);

                entity.HasOne<Empresa>()
                      .WithMany()
                      .HasForeignKey(r => r.EmpresaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
