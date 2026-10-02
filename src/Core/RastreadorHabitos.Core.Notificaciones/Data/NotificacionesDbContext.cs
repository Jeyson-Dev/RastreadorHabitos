using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Notificaciones.Entities;

namespace RastreadorHabitos.Core.Notificaciones.Data;

public class NotificacionesDbContext : DbContext
{
    // Cada pieza es dueña de sus tablas dentro de su propio esquema SQL.
    public const string Esquema = "Notificaciones";

    public NotificacionesDbContext(DbContextOptions<NotificacionesDbContext> options) : base(options)
    {
    }

    public DbSet<CorreoEnCola> CorreosEnCola => Set<CorreoEnCola>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);

        modelBuilder.Entity<CorreoEnCola>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Destinatario).IsRequired().HasMaxLength(256);
            builder.Property(c => c.Asunto).IsRequired().HasMaxLength(256);
            builder.Property(c => c.CuerpoHtml).IsRequired();
            builder.Property(c => c.Estado).IsRequired().HasMaxLength(20);

            // El enviador toma los pendientes en orden de llegada.
            builder.HasIndex(c => new { c.Estado, c.FechaCreacionUtc });
        });
    }
}
