using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Entities;

namespace RastreadorHabitos.Tests.GestionPermisos;

// Transiciones válidas: Pendiente → Aprobada → Aplicada y Pendiente → Rechazada.
// [RF-GP-03] Al aprobar queda registrado quién aprobó y cuándo.
// [RF-GP-04] Al rechazar queda registrado el motivo.
// [RF-GP-05] Al aprobar, el sistema aplica el permiso automáticamente.
public class TransicionValidaTests
{
    private static readonly Guid SolicitanteId = Guid.NewGuid();
    private static readonly Guid AdministradorId = Guid.NewGuid();

    private static async Task<Guid> CrearPendienteAsync(BaseEnMemoria baseEnMemoria)
    {
        var creada = await baseEnMemoria.NuevoServicio()
            .CrearAsync(SolicitanteId, new CreacionSolicitudPermisoDto { Permiso = "Administrador" });
        return creada.Id;
    }

    // [RF-GP-03] Al aprobar queda registrado quién aprobó y cuándo.
    [Fact]
    public async Task Aprobar_SolicitudPendiente_QuedaAplicadaYRegistraQuienAproboYCuando()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPendienteAsync(baseEnMemoria);
        var antes = DateTime.UtcNow;

        // Actuar
        await baseEnMemoria.NuevoServicio().AprobarAsync(solicitudId, AdministradorId);

        // Verificar
        var despues = DateTime.UtcNow;
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Aplicada, guardada!.Estado);
        Assert.Equal(AdministradorId, guardada.ResueltaPorId);
        Assert.InRange(guardada.FechaResolucionUtc!.Value, antes, despues);
    }

    // [RF-GP-05] Tras aprobar, la solicitud llega a Aplicada y el permiso pedido queda aplicado al
    // solicitante, sin intervención manual.
    [Fact]
    public async Task Aprobar_SolicitudPendiente_LlegaAAplicadaYAplicaElPermisoPedidoSinIntervencionManual()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPendienteAsync(baseEnMemoria);
        var aplicador = new AplicadorPermisoFalso();

        // Actuar
        await baseEnMemoria.NuevoServicio(aplicador).AprobarAsync(solicitudId, AdministradorId);

        // Verificar
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Aplicada, guardada!.Estado);
        var aplicado = Assert.Single(aplicador.Aplicados);
        Assert.Equal((SolicitanteId, "Administrador", AdministradorId), aplicado);
    }

    // [RF-GP-04] Al rechazar con motivo queda Rechazada y registra el motivo.
    [Fact]
    public async Task Rechazar_SolicitudPendienteConMotivo_QuedaRechazadaYRegistraElMotivo()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPendienteAsync(baseEnMemoria);

        // Actuar
        await baseEnMemoria.NuevoServicio().RechazarAsync(solicitudId, AdministradorId,
            new RechazoSolicitudPermisoDto { Motivo = "No necesita administrar usuarios." });

        // Verificar
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Rechazada, guardada!.Estado);
        Assert.Equal("No necesita administrar usuarios.", guardada.MotivoRechazo);
        Assert.Equal(AdministradorId, guardada.ResueltaPorId);
    }
}
