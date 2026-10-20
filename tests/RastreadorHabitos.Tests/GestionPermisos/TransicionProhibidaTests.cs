using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Reglas;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Tests.GestionPermisos;

// [RF-GP-06] Solo se permiten Pendiente → Aprobada → Aplicada y Pendiente → Rechazada. Aprobar
// dos veces la misma solicitud, o pasar de Rechazada a Aplicada, se rechaza y el estado no cambia.
public class TransicionProhibidaTests
{
    private static readonly Guid SolicitanteId = Guid.NewGuid();
    private static readonly Guid AdministradorId = Guid.NewGuid();
    private static readonly Guid OtroAdministradorId = Guid.NewGuid();

    private static async Task<Guid> CrearPendienteAsync(BaseEnMemoria baseEnMemoria)
    {
        var creada = await baseEnMemoria.NuevoServicio()
            .CrearAsync(SolicitanteId, new CreacionSolicitudPermisoDto { Permiso = "Administrador" });
        return creada.Id;
    }

    [Fact]
    public async Task Aprobar_SolicitudYaAplicada_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPendienteAsync(baseEnMemoria);
        await baseEnMemoria.NuevoServicio().AprobarAsync(solicitudId, AdministradorId);

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoGestionPermisosException>(() =>
            baseEnMemoria.NuevoServicio().AprobarAsync(solicitudId, OtroAdministradorId));

        // Verificar
        Assert.Equal(TipoRechazoGestionPermisos.TransicionNoPermitida, rechazo.Tipo);
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Aplicada, guardada!.Estado);
        Assert.Equal(AdministradorId, guardada.ResueltaPorId);
    }

    [Fact]
    public async Task Aprobar_SolicitudRechazada_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPendienteAsync(baseEnMemoria);
        await baseEnMemoria.NuevoServicio().RechazarAsync(solicitudId, AdministradorId,
            new RechazoSolicitudPermisoDto { Motivo = "No corresponde." });
        var aplicador = new AplicadorPermisoFalso();

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoGestionPermisosException>(() =>
            baseEnMemoria.NuevoServicio(aplicador).AprobarAsync(solicitudId, AdministradorId));

        // Verificar
        Assert.Equal(TipoRechazoGestionPermisos.TransicionNoPermitida, rechazo.Tipo);
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Rechazada, guardada!.Estado);
        Assert.Empty(aplicador.Aplicados);
    }

    [Fact]
    public void MarcarAplicada_SolicitudRechazada_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var solicitud = new SolicitudPermiso(SolicitanteId, PermisosSolicitables.Administrador, DateTime.UtcNow);
        solicitud.Rechazar(AdministradorId, "No corresponde.", DateTime.UtcNow);

        // Actuar
        var rechazo = Assert.Throws<RechazoGestionPermisosException>(solicitud.MarcarAplicada);

        // Verificar
        Assert.Equal(TipoRechazoGestionPermisos.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
    }

    [Fact]
    public async Task Rechazar_SinMotivoEscrito_NoSeCompletaYLaSolicitudSiguePendiente()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPendienteAsync(baseEnMemoria);

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoGestionPermisosException>(() =>
            baseEnMemoria.NuevoServicio().RechazarAsync(solicitudId, AdministradorId,
                new RechazoSolicitudPermisoDto { Motivo = "   " }));

        // Verificar
        Assert.Equal(TipoRechazoGestionPermisos.DatosInvalidos, rechazo.Tipo);
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada!.Estado);
    }

    // Todas las parejas que no están en la tabla, incluidas las que salen de un estado final.
    [Theory]
    [InlineData(EstadoSolicitud.Pendiente, EstadoSolicitud.Aplicada)]
    [InlineData(EstadoSolicitud.Aprobada, EstadoSolicitud.Rechazada)]
    [InlineData(EstadoSolicitud.Aprobada, EstadoSolicitud.Pendiente)]
    [InlineData(EstadoSolicitud.Aplicada, EstadoSolicitud.Aprobada)]
    [InlineData(EstadoSolicitud.Aplicada, EstadoSolicitud.Pendiente)]
    [InlineData(EstadoSolicitud.Aplicada, EstadoSolicitud.Rechazada)]
    [InlineData(EstadoSolicitud.Rechazada, EstadoSolicitud.Aplicada)]
    [InlineData(EstadoSolicitud.Rechazada, EstadoSolicitud.Aprobada)]
    [InlineData(EstadoSolicitud.Rechazada, EstadoSolicitud.Pendiente)]
    public void TransicionFueraDeLaTabla_NoEstaPermitida(EstadoSolicitud desde, EstadoSolicitud hacia)
    {
        // Preparar: la pareja (desde, hacia) llega por parámetro.

        // Actuar
        var permitida = TransicionesSolicitud.EsPermitida(desde, hacia);

        // Verificar
        Assert.False(permitida);
    }
}
