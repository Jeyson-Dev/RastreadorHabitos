using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Documentos.Entities;

namespace RastreadorHabitos.Core.Documentos.Data;

public class DocumentosDbContext : DbContext
{
    // Cada pieza es dueña de sus tablas dentro de su propio esquema SQL.
    public const string Esquema = "Documentos";

    public DocumentosDbContext(DbContextOptions<DocumentosDbContext> options) : base(options)
    {
    }

    public DbSet<Documento> Documentos => Set<Documento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);

        modelBuilder.Entity<Documento>(builder =>
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Nombre).IsRequired().HasMaxLength(255);
            builder.Property(d => d.Tipo).IsRequired().HasMaxLength(255);
            builder.Property(d => d.RutaAlmacenamiento).IsRequired().HasMaxLength(100);

            // Sin relación con las tablas de Control de acceso: solo el Id de quien lo subió.
            // Para listar rápido los documentos de un usuario [RF-DOC-02].
            builder.HasIndex(d => new { d.SubidoPorId, d.Eliminado });
        });
    }
}
