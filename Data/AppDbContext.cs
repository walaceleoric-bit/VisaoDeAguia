using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VisaoDeAguia.Models;

namespace VisaoDeAguia.Data
{
    public class AppDbContext : IdentityDbContext<Usuario>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<ConfiguracaoRobo> ConfiguracoesRobo { get; set; }

        public DbSet<SinalEnviado> SinaisEnviados { get; set; }

        public DbSet<ConsumoDiarioRobo> ConsumosDiariosRobo { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =========================================
            // CONFIGURAÇÃO DO ROBÔ
            // =========================================

            builder.Entity<ConfiguracaoRobo>()
                .HasIndex(c => c.UsuarioId)
                .IsUnique();

            builder.Entity<ConfiguracaoRobo>()
                .HasOne(c => c.Usuario)
                .WithOne()
                .HasForeignKey<ConfiguracaoRobo>(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================================
            // HISTÓRICO DE SINAIS
            // =========================================

            builder.Entity<SinalEnviado>()
                .HasOne(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Impede que o mesmo sinal da mesma vela
            // seja registrado duas vezes para o mesmo usuário.
            builder.Entity<SinalEnviado>()
                .HasIndex(s => new
                {
                    s.UsuarioId,
                    s.Simbolo,
                    s.Direcao,
                    s.DataHoraVela
                })
                .IsUnique();

            // =========================================
            // CONSUMO DIÁRIO DO ROBÔ
            // =========================================

            builder.Entity<ConsumoDiarioRobo>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cada usuário terá apenas um registro
            // de consumo para cada dia.
            builder.Entity<ConsumoDiarioRobo>()
                .HasIndex(c => new
                {
                    c.UsuarioId,
                    c.Data
                })
                .IsUnique();
        }
    }
}