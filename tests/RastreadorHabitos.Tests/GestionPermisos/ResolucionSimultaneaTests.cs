using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Tests.GestionPermisos;

// [RF-GP-06] Aprobar dos veces la misma solicitud se rechaza y el estado no cambia, también
// cuando las dos operaciones la leyeron Pendiente antes de que cualquiera guardara. Se reproduce
// sin hilos: un contexto lee la solicitud, otra operación la resuelve y el primero intenta guardar.
public class ResolucionSimultaneaTests
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

    // [RF-GP-06] Aprobar dos veces, aunque las dos la leyeran Pendiente, se rechaza.
    [Fact]
    public async Task Aprobar_SolicitudAprobadaPorOtraOperacionDespuesDeLeerla_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPendienteAsync(baseEnMemoria);
        var contextoDesactualizado = baseEnMemoria.NuevoContexto();
        await contextoDesactualizado.SolicitudesPermiso.FindAsync(solicitudId);
        await baseEnMemoria.NuevoServicio().AprobarAsync(solicitudId, AdministradorId);
        var aplicador = new AplicadorPermisoFalso();

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoGestionPermisosException>(() =>
            BaseEnMemoria.NuevoServicio(contextoDesactualizado, aplicador).AprobarAsync(solicitudId, OtroAdministradorId));

        // Verificar
        Assert.Equal(TipoRechazoGestionPermisos.TransicionNoPermitida, rechazo.Tipo);
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Aplicada, guardada!.Estado);
        Assert.Equal(AdministradorId, guardada.ResueltaPorId);
        Assert.Empty(aplicador.Aplicados);
    }

    // [RF-GP-06] Rechazar una que otra operación ya aprobó se rechaza.
    [Fact]
    public async Task Rechazar_SolicitudAprobadaPorOtraOperacionDespuesDeLeerla_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPendienteAsync(baseEnMemoria);
        var contextoDesactualizado = baseEnMemoria.NuevoContexto();
        await contextoDesactualizado.SolicitudesPermiso.FindAsync(solicitudId);
        await baseEnMemoria.NuevoServicio().AprobarAsync(solicitudId, AdministradorId);

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoGestionPermisosException>(() =>
            BaseEnMemoria.NuevoServicio(contextoDesactualizado).RechazarAsync(solicitudId, OtroAdministradorId,
                new RechazoSolicitudPermisoDto { Motivo = "Llegó tarde." }));

        // Verificar
        Assert.Equal(TipoRechazoGestionPermisos.TransicionNoPermitida, rechazo.Tipo);
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Aplicada, guardada!.Estado);
        Assert.Null(guardada.MotivoRechazo);
    }
}
