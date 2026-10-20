using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.GestionPermisos.Entities;

namespace RastreadorHabitos.Core.GestionPermisos.Data;

public class GestionPermisosDbContext : DbContext
{
    // Cada pieza es dueña de sus tablas dentro de su propio esquema SQL.
    public const string Esquema = "GestionPermisos";

    public GestionPermisosDbContext(DbContextOptions<GestionPermisosDbContext> options) : base(options)
    {
    }

    public DbSet<SolicitudPermiso> SolicitudesPermiso => Set<SolicitudPermiso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);

        modelBuilder.Entity<SolicitudPermiso>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Permiso).IsRequired().HasMaxLength(50);

            // El estado se guarda con su nombre ("Pendiente"), legible al consultar la base.
            builder.Property(s => s.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

            // Sin relación con las tablas de Control de acceso: solo los Id de solicitante y resolutor.
            // Para listar rápido las pendientes [RF-GP-02].
            builder.HasIndex(s => s.Estado);
        });
    }
}
