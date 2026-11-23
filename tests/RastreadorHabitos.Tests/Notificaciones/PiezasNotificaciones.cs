using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RastreadorHabitos.Api.Adaptadores;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.GestionPermisos.Services;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.DTOs;
using RastreadorHabitos.Core.Notificaciones.Entities;
using RastreadorHabitos.Core.Notificaciones.Services;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.Services;
using RastreadorHabitos.Modulo.Metas.Data;
using RastreadorHabitos.Modulo.Metas.Services;

namespace RastreadorHabitos.Tests.Notificaciones;

// Notificaciones con las piezas reales que la usan (Control de acceso, Gestión de permisos y el
// módulo de negocio), conectadas con los mismos adaptadores de la Api y sin levantarla [RD-12].
// Cada instancia usa bases en memoria con nombre único: ninguna prueba depende de otra.
public sealed class PiezasNotificaciones
{
    private readonly string _sufijo = Guid.NewGuid().ToString("N");

    public PiezasNotificaciones(TimeProvider? reloj = null)
    {
        Reloj = reloj ?? TimeProvider.System;
        using var contexto = ControlAcceso();
        contexto.Database.EnsureCreated(); // los dos roles del sistema
    }

    public TimeProvider Reloj { get; }

    public ControlAccesoDbContext ControlAcceso() =>
        new(new DbContextOptionsBuilder<ControlAccesoDbContext>().UseInMemoryDatabase($"CA-{_sufijo}").Options);

    public NotificacionesDbContext NotificacionesContexto() =>
        new(new DbContextOptionsBuilder<NotificacionesDbContext>().UseInMemoryDatabase($"NOT-{_sufijo}").Options);

    private GestionPermisosDbContext GestionPermisosContexto() =>
        new(new DbContextOptionsBuilder<GestionPermisosDbContext>()
            .UseInMemoryDatabase($"GP-{_sufijo}")
            .ConfigureWarnings(avisos => avisos.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private HabitosDbContext HabitosContexto() =>
        new(new DbContextOptionsBuilder<HabitosDbContext>().UseInMemoryDatabase($"HAB-{_sufijo}").Options);

    private MetasDbContext MetasContexto() =>
        new(new DbContextOptionsBuilder<MetasDbContext>().UseInMemoryDatabase($"MET-{_sufijo}").Options);

    // Un usuario con el rol indicado. Activo = correo confirmado y cuenta habilitada.
    public async Task<Guid> CrearUsuarioAsync(int rolId, bool correoConfirmado = true, bool cuentaHabilitada = true)
    {
        await using var contexto = ControlAcceso();
        var usuario = new Usuario
        {
            Email = $"{Guid.NewGuid():N}@prueba.local",
            NombreCompleto = "Usuario de prueba",
            PasswordHash = "no-se-usa-en-esta-prueba",
            RolId = rolId,
            CorreoConfirmado = correoConfirmado,
            CuentaHabilitada = cuentaHabilitada
        };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }

    public async Task<string> CorreoDeAsync(Guid usuarioId)
    {
        await using var contexto = ControlAcceso();
        return (await contexto.Usuarios.FindAsync(usuarioId))!.Email;
    }

    // notificar, como lo registra la Api.
    public Notificador Notificador() =>
        new(NotificacionesContexto(), new DirectorioDestinatariosPorControlAcceso(new DirectorioUsuariosService(ControlAcceso())));

    public NotificacionesService Bandeja() => new(NotificacionesContexto());

    // Gestión de permisos con su aplicador y su aviso reales, como en la Api.
    public SolicitudesPermisoService GestionPermisos()
    {
        var controlAcceso = ControlAcceso();
        var aplicador = new AplicadorPermisoPorControlAcceso(
            new AdministracionUsuariosService(controlAcceso), new PermisosUsuarioService(controlAcceso));
        var aviso = new AvisoSolicitudesPorNotificaciones(new DirectorioUsuariosService(ControlAcceso()), Notificador());
        return new SolicitudesPermisoService(GestionPermisosContexto(), aplicador, aviso);
    }

    public HabitosService Habitos() =>
        new(HabitosContexto(), new PermisosUsuarioService(ControlAcceso()), Reloj);

    public MetasService Metas() => new(MetasContexto(), Habitos(), Reloj, Notificador());

    public async Task<List<Notificacion>> NotificacionesDeAsync(Guid usuarioId)
    {
        await using var contexto = NotificacionesContexto();
        return await contexto.Notificaciones.Where(n => n.DestinatarioId == usuarioId).ToListAsync();
    }

    public async Task<int> ContarNotificacionesAsync()
    {
        await using var contexto = NotificacionesContexto();
        return await contexto.Notificaciones.CountAsync();
    }

    public async Task<List<CorreoEnCola>> CorreosAsync()
    {
        await using var contexto = NotificacionesContexto();
        return await contexto.CorreosEnCola.ToListAsync();
    }

    public Task DesactivarCorreoAsync(Guid usuarioId) =>
        Bandeja().ActualizarPreferenciasAsync(usuarioId, new ActualizacionPreferenciasDto { CorreoActivo = false });
}

// Reloj en una fecha y hora locales dadas, para que «hoy» no dependa del día en que corre la
// prueba. Se puede adelantar para simular que pasan los días.
public sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    private readonly TimeZoneInfo _zona = TimeZoneInfo.CreateCustomTimeZone("Prueba", ahora.Offset, "Prueba", "Prueba");

    public DateTimeOffset Ahora { get; private set; } = ahora;

    public void AdelantarDias(int dias) => Ahora = Ahora.AddDays(dias);

    public override DateTimeOffset GetUtcNow() => Ahora.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => _zona;
}
