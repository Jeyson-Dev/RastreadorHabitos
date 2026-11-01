using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Modulo.Habitos.Entities;

namespace RastreadorHabitos.Modulo.Habitos.Data;

public class HabitosDbContext : DbContext
{
    // Cada pieza es dueña de sus tablas dentro de su propio esquema SQL.
    public const string Esquema = "Habitos";

    public HabitosDbContext(DbContextOptions<HabitosDbContext> options) : base(options)
    {
    }

    public DbSet<Habito> Habitos => Set<Habito>();
    public DbSet<RegistroDiario> RegistrosDiarios => Set<RegistroDiario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);

        modelBuilder.Entity<Habito>(builder =>
        {
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(h => h.Descripcion).HasMaxLength(500);

            // Sin relación con las tablas de Control de acceso: solo el Id del dueño.
            builder.HasIndex(h => h.UsuarioId);

            builder.HasMany(h => h.Dias).WithOne().HasForeignKey(d => d.HabitoId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(h => h.Registros).WithOne().HasForeignKey(r => r.HabitoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HabitoDia>(builder =>
        {
            builder.ToTable("HabitosDias", tabla => tabla.HasCheckConstraint(
                "CK_HabitosDias_DiaSemana", "[DiaSemana] BETWEEN 1 AND 7"));
            builder.HasKey(d => new { d.HabitoId, d.DiaSemana });
        });

        modelBuilder.Entity<RegistroDiario>(builder =>
        {
            builder.HasKey(r => r.Id);

            // Un solo registro por hábito y día.
            builder.HasIndex(r => new { r.HabitoId, r.Fecha }).IsUnique();
        });
    }
}
