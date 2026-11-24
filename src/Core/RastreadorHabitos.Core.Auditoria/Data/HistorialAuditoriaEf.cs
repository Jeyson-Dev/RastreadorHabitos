using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RastreadorHabitos.Core.Auditoria.Entities;

namespace RastreadorHabitos.Core.Auditoria.Data;

// Lo que necesita el DbContext de una pieza auditada para escribir su registro en el MISMO
// guardado que su cambio: así no puede quedar un cambio sin su registro, ni un registro sin su
// cambio, porque los dos entran o ninguno en la misma transacción local [RF-AUD-02 a 05].
public static class HistorialAuditoriaEf
{
    public const string Esquema = "Auditoria";
    public const string Tabla = "RegistrosAuditoria";

    // Mapea la tabla del historial. Solo AuditoriaDbContext la crea en sus migraciones; los demás
    // contextos la usan sin crearla (excluirDeMigraciones: true).
    public static ModelBuilder MapearHistorialAuditoria(this ModelBuilder modelBuilder, bool excluirDeMigraciones)
    {
        modelBuilder.Entity<RegistroAuditoria>(builder =>
        {
            builder.ToTable(Tabla, Esquema, tabla =>
            {
                if (excluirDeMigraciones)
                {
                    tabla.ExcludeFromMigrations();
                }
            });
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedNever();
            builder.Property(r => r.Accion).IsRequired().HasMaxLength(60);
            builder.Property(r => r.Entidad).IsRequired().HasMaxLength(60);
            builder.Property(r => r.EntidadId).IsRequired().HasMaxLength(100);
            builder.Property(r => r.ValorAnterior).IsRequired();
            builder.Property(r => r.ValorNuevo).IsRequired();

            // UTC al guardar y al leer, igual que el resto del sistema [RD-11].
            builder.Property(r => r.FechaUtc).HasConversion(
                new ValueConverter<DateTime, DateTime>(f => f, f => DateTime.SpecifyKind(f, DateTimeKind.Utc)));

            // Para filtrar en el motor de datos por usuario, por entidad y por fecha [RF-AUD-07].
            builder.HasIndex(r => r.FechaUtc);
            builder.HasIndex(r => new { r.UsuarioId, r.FechaUtc });
            builder.HasIndex(r => new { r.Entidad, r.FechaUtc });
        });
        return modelBuilder;
    }

    // Activa la protección del historial en el contexto [RF-AUD-06].
    public static DbContextOptionsBuilder ProtegerHistorialAuditoria(this DbContextOptionsBuilder opciones) =>
        opciones.AddInterceptors(HistorialInmutable.Instancia);

    // registrarEvento: agrega el registro al contexto de la pieza, SIN guardar. Se guarda con el
    // próximo SaveChanges, junto con el cambio que describe.
    public static void RegistrarEvento(this DbContext contexto, Guid usuarioId, string accion, string entidad,
                                       string entidadId, object? anterior, object? nuevo, DateTime ahoraUtc) =>
        contexto.Set<RegistroAuditoria>().Add(
            RegistroAuditoria.Crear(usuarioId, accion, entidad, entidadId, anterior, nuevo, ahoraUtc));
}

// Ningún guardado puede modificar ni borrar un registro del historial, venga de donde venga: la
// aplicación no tiene esa operación y, si algún código lo intentara, el guardado se rechaza entero
// [RF-AUD-06].
public sealed class HistorialInmutable : SaveChangesInterceptor
{
    public static readonly HistorialInmutable Instancia = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Verificar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Verificar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Verificar(DbContext? contexto)
    {
        if (contexto is null)
        {
            return;
        }

        if (contexto.ChangeTracker.Entries<RegistroAuditoria>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("El historial de auditoría no se modifica ni se elimina.");
        }
    }
}
