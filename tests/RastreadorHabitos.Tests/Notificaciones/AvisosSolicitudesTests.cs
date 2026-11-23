using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Reglas;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Tests.Notificaciones;

// Gestión de permisos avisa al crear y al resolver una solicitud, con las piezas reales.
public class AvisosSolicitudesTests
{
    private static CreacionSolicitudPermisoDto Pedir(string permiso = PermisosSolicitables.Administrador) =>
        new() { Permiso = permiso };

    // [RF-NOT-01] CN-01: crear una solicitud notifica a cada Administrador activo y a nadie más.
    [Fact]
    public async Task CrearSolicitud_NotificaACadaAdministradorActivoYANingunEstandar()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var a1 = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        var a2 = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        var e1 = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var e2 = await piezas.CrearUsuarioAsync(Rol.IdEstandar);

        // Actuar
        await piezas.GestionPermisos().CrearAsync(e1, Pedir());

        // Verificar
        foreach (var administrador in new[] { a1, a2 })
        {
            var aviso = Assert.Single(await piezas.NotificacionesDeAsync(administrador));
            Assert.Equal(AvisosSolicitud.TipoCreada, aviso.Tipo);
            Assert.False(aviso.Leida);
            Assert.Contains(PermisosSolicitables.Administrador, aviso.Mensaje);
        }
        Assert.Empty(await piezas.NotificacionesDeAsync(e1));
        Assert.Empty(await piezas.NotificacionesDeAsync(e2));
    }

    // [RF-NOT-01] CN-02: un Administrador desactivado o que no activó su cuenta no la recibe.
    [Fact]
    public async Task CrearSolicitud_NoNotificaAAdministradoresDesactivadosNiSinActivar()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var activo = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        var desactivado = await piezas.CrearUsuarioAsync(Rol.IdAdministrador, cuentaHabilitada: false);
        var sinActivar = await piezas.CrearUsuarioAsync(Rol.IdAdministrador, correoConfirmado: false);
        var solicitante = await piezas.CrearUsuarioAsync(Rol.IdEstandar);

        // Actuar
        await piezas.GestionPermisos().CrearAsync(solicitante, Pedir());

        // Verificar
        Assert.Single(await piezas.NotificacionesDeAsync(activo));
        Assert.Empty(await piezas.NotificacionesDeAsync(desactivado));
        Assert.Empty(await piezas.NotificacionesDeAsync(sinActivar));
    }

    // [RF-NOT-01] CN-03: sin ningún Administrador activo, la solicitud se crea y nadie es notificado.
    [Fact]
    public async Task CrearSolicitud_SinAdministradoresActivos_SeCreaPendienteYNoNotificaANadie()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var solicitante = await piezas.CrearUsuarioAsync(Rol.IdEstandar);

        // Actuar
        var creada = await piezas.GestionPermisos().CrearAsync(solicitante, Pedir());

        // Verificar
        Assert.Equal("Pendiente", creada.Estado);
        Assert.Equal(0, await piezas.ContarNotificacionesAsync());
        Assert.Empty(await piezas.CorreosAsync());
    }

    // [RF-NOT-01, RF-GP-07] CN-04: un Administrador que pide un permiso no se avisa a sí mismo.
    [Fact]
    public async Task CrearSolicitud_DeUnAdministrador_NotificaALosDemasYNoASiMismo()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var solicitante = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        var otro = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);

        // Actuar
        await piezas.GestionPermisos().CrearAsync(solicitante, Pedir(PermisosSolicitables.LimiteHabitosAmpliado));

        // Verificar
        Assert.Empty(await piezas.NotificacionesDeAsync(solicitante));
        Assert.Single(await piezas.NotificacionesDeAsync(otro));
    }

    // [RF-NOT-02] CN-05: aprobar notifica al solicitante que fue aprobada, y a nadie más.
    [Fact]
    public async Task AprobarSolicitud_NotificaAlSolicitanteQueFueAprobada()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var administrador = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        var solicitante = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var otro = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var solicitud = await piezas.GestionPermisos().CrearAsync(solicitante, Pedir(PermisosSolicitables.LimiteHabitosAmpliado));

        // Actuar
        await piezas.GestionPermisos().AprobarAsync(solicitud.Id, administrador);

        // Verificar
        var aviso = Assert.Single(await piezas.NotificacionesDeAsync(solicitante));
        Assert.Equal(AvisosSolicitud.TipoAprobada, aviso.Tipo);
        Assert.Contains("aprobada", aviso.Mensaje);
        Assert.Contains(PermisosSolicitables.LimiteHabitosAmpliado, aviso.Mensaje);
        Assert.Empty(await piezas.NotificacionesDeAsync(otro));
        Assert.DoesNotContain(await piezas.NotificacionesDeAsync(administrador), n => n.Tipo == AvisosSolicitud.TipoAprobada);
    }

    // [RF-NOT-02] CN-06: rechazar notifica al solicitante que fue rechazada, con el motivo literal.
    [Fact]
    public async Task RechazarSolicitud_NotificaAlSolicitanteConElMotivo()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var administrador = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        var solicitante = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var solicitud = await piezas.GestionPermisos().CrearAsync(solicitante, Pedir());

        // Actuar
        await piezas.GestionPermisos().RechazarAsync(solicitud.Id, administrador,
            new RechazoSolicitudPermisoDto { Motivo = "No necesita administrar usuarios." });

        // Verificar
        var aviso = Assert.Single(await piezas.NotificacionesDeAsync(solicitante));
        Assert.Equal(AvisosSolicitud.TipoRechazada, aviso.Tipo);
        Assert.Contains("rechazada", aviso.Mensaje);
        Assert.Contains("No necesita administrar usuarios.", aviso.Mensaje);
    }

    // [RF-NOT-02] CN-07: una resolución que no ocurre (aprobar dos veces, rechazar sin motivo) no notifica.
    [Fact]
    public async Task ResolucionRechazada_NoNotificaAlSolicitante()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var administrador = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        var solicitante = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var solicitud = await piezas.GestionPermisos().CrearAsync(solicitante, Pedir(PermisosSolicitables.LimiteHabitosAmpliado));
        await piezas.GestionPermisos().AprobarAsync(solicitud.Id, administrador);
        var antes = (await piezas.NotificacionesDeAsync(solicitante)).Count;

        // Actuar
        await Assert.ThrowsAsync<RechazoGestionPermisosException>(
            () => piezas.GestionPermisos().AprobarAsync(solicitud.Id, administrador));
        await Assert.ThrowsAsync<RechazoGestionPermisosException>(
            () => piezas.GestionPermisos().RechazarAsync(solicitud.Id, administrador, new RechazoSolicitudPermisoDto()));

        // Verificar
        Assert.Equal(antes, (await piezas.NotificacionesDeAsync(solicitante)).Count);
    }
}
