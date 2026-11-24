using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Auditoria.Data;
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

    // El historial de auditoría no se modifica ni se borra desde este contexto [RF-AUD-06].
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ProtegerHistorialAuditoria();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);

        // El registro de auditoría se escribe en el mismo guardado que la subida o la eliminación;
        // la tabla la crea la migración de Auditoría, no la de esta pieza [RF-DOC-06, RF-AUD-03].
        modelBuilder.MapearHistorialAuditoria(excluirDeMigraciones: true);

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
