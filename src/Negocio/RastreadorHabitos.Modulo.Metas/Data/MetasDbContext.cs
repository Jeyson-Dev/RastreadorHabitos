using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Modulo.Metas.Entities;

namespace RastreadorHabitos.Modulo.Metas.Data;

public class MetasDbContext : DbContext
{
    // Cada pieza es dueña de sus tablas dentro de su propio esquema SQL.
    public const string Esquema = "Metas";

    public MetasDbContext(DbContextOptions<MetasDbContext> options) : base(options)
    {
    }

    public DbSet<Meta> Metas => Set<Meta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);

        modelBuilder.Entity<Meta>(builder =>
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Nombre).IsRequired().HasMaxLength(150);

            // El estado se guarda con su nombre ("EnProgreso"), legible al consultar la base.
            builder.Property(m => m.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

            // Sin relación con las tablas de Control de acceso: solo el Id del dueño.
            builder.HasIndex(m => m.UsuarioId);

            builder.ToTable(tabla => tabla.HasCheckConstraint(
                "CK_Metas_ObjetivoCumplimientos", "[ObjetivoCumplimientos] >= 1"));
        });
    }
}
