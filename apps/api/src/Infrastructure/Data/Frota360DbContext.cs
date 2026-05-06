using Frota360.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Data
{
    public class Frota360DbContext(DbContextOptions<Frota360DbContext> options) : DbContext(options)
    {
        public DbSet<Veiculo> Veiculos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Motorista> Motoristas { get; set; }
        public DbSet<Rota> Rotas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Nome).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
                entity.HasIndex(u => u.Email).IsUnique(); 
                entity.Property(u => u.SenhaHash).IsRequired();
                entity.Property(u => u.DataInclusao).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Motorista>(entity =>
            {
                entity.ToTable("Motorista");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Nome).HasMaxLength(100).IsRequired();
                entity.Property(m => m.Email).HasMaxLength(150).IsRequired();
                entity.HasIndex(m => m.Email).IsUnique();
                entity.Property(m => m.CPF).HasMaxLength(11).IsRequired();
                entity.HasIndex(m => m.CPF).IsUnique();
                entity.Property(m => m.DataInclusao).HasDefaultValueSql("GETDATE()");
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
            });
        }
    }
}
