using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Tests.GestionPermisos;

// [RF-GP-07] Nadie resuelve su propia solicitud: un Administrador que intenta aprobar o rechazar
// una solicitud creada por él mismo recibe un rechazo.
public class ResolucionPropiaTests
{
    private static readonly Guid AdministradorId = Guid.NewGuid();

    private static async Task<Guid> CrearPropiaAsync(BaseEnMemoria baseEnMemoria)
    {
        var creada = await baseEnMemoria.NuevoServicio()
            .CrearAsync(AdministradorId, new CreacionSolicitudPermisoDto { Permiso = "Administrador" });
        return creada.Id;
    }

    [Fact]
    public async Task Aprobar_SolicitudCreadaPorElMismoAdministrador_SeRechazaYSiguePendiente()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPropiaAsync(baseEnMemoria);

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoGestionPermisosException>(() =>
            baseEnMemoria.NuevoServicio().AprobarAsync(solicitudId, AdministradorId));

        // Verificar
        Assert.Equal(TipoRechazoGestionPermisos.NoPermitido, rechazo.Tipo);
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada!.Estado);
    }

    [Fact]
    public async Task Rechazar_SolicitudCreadaPorElMismoAdministrador_SeRechazaYSiguePendiente()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var solicitudId = await CrearPropiaAsync(baseEnMemoria);

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoGestionPermisosException>(() =>
            baseEnMemoria.NuevoServicio().RechazarAsync(solicitudId, AdministradorId,
                new RechazoSolicitudPermisoDto { Motivo = "Me la rechazo yo mismo." }));

        // Verificar
        Assert.Equal(TipoRechazoGestionPermisos.NoPermitido, rechazo.Tipo);
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(solicitudId);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada!.Estado);
    }
}
