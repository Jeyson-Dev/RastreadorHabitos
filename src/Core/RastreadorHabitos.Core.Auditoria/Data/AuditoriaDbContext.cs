using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Auditoria.Entities;

namespace RastreadorHabitos.Core.Auditoria.Data;

// Dueño de la tabla del historial: crea el esquema Auditoria en sus migraciones y la consulta. Las
// piezas auditadas escriben en ella desde su propio contexto, en el mismo guardado de su cambio.
public class AuditoriaDbContext : DbContext
{
    public const string Esquema = HistorialAuditoriaEf.Esquema;

    public AuditoriaDbContext(DbContextOptions<AuditoriaDbContext> options) : base(options)
    {
    }

    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ProtegerHistorialAuditoria();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.MapearHistorialAuditoria(excluirDeMigraciones: false);
    }
}
