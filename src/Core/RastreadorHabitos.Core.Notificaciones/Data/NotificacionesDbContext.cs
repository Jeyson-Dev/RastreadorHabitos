using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<PreferenciaNotificacion> PreferenciasNotificacion => Set<PreferenciaNotificacion>();

    // Todas las fechas de la pieza se guardan en UTC; al leerlas de la base se marcan como UTC,
    // así salen en JSON con la «Z» igual que en el resto del sistema [RD-11].
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<FechaUtcConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<FechaUtcOpcionalConverter>();
    }

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

        modelBuilder.Entity<Notificacion>(builder =>
        {
            builder.ToTable("Notificaciones");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Tipo).IsRequired().HasMaxLength(50);
            builder.Property(n => n.Mensaje).IsRequired();

            // Sin relación con las tablas de Control de acceso: solo el Id del destinatario.
            // Para leer rápido la bandeja de un usuario y contar sus no leídas [RF-NOT-04].
            builder.HasIndex(n => new { n.DestinatarioId, n.Leida, n.FechaUtc });
        });

        modelBuilder.Entity<PreferenciaNotificacion>(builder =>
        {
            builder.ToTable("PreferenciasNotificacion");

            // Una fila por usuario [RF-NOT-07].
            builder.HasKey(p => p.UsuarioId);
            builder.Property(p => p.UsuarioId).ValueGeneratedNever();
        });
    }

    private sealed class FechaUtcConverter() : ValueConverter<DateTime, DateTime>(
        fecha => fecha, fecha => DateTime.SpecifyKind(fecha, DateTimeKind.Utc));

    private sealed class FechaUtcOpcionalConverter() : ValueConverter<DateTime?, DateTime?>(
        fecha => fecha, fecha => fecha.HasValue ? DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc) : fecha);
}
