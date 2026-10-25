using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Tests.GestionPermisos;

// [RF-GP-01] Un usuario Estándar crea una solicitud de permiso: nace en estado Pendiente.
// [RD-07] Una entrada inválida produce un rechazo controlado.
public class CreacionSolicitudTests
{
    [Fact]
    public async Task Crear_ConPermisoValido_LaSolicitudNaceEnEstadoPendiente()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var servicio = baseEnMemoria.NuevoServicio();
        var solicitanteId = Guid.NewGuid();

        // Actuar
        var creada = await servicio.CrearAsync(solicitanteId,
            new CreacionSolicitudPermisoDto { Permiso = "Administrador" });

        // Verificar
        var guardada = await baseEnMemoria.NuevoContexto().SolicitudesPermiso.FindAsync(creada.Id);
        Assert.NotNull(guardada);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada.Estado);
        Assert.Equal(solicitanteId, guardada.SolicitanteId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SuperUsuario")]
    public async Task Crear_ConPermisoVacioOFueraDelCatalogo_SeRechazaConDatosInvalidos(string? permiso)
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var servicio = baseEnMemoria.NuevoServicio();

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoGestionPermisosException>(() =>
            servicio.CrearAsync(Guid.NewGuid(), new CreacionSolicitudPermisoDto { Permiso = permiso }));

        // Verificar
        Assert.Equal(TipoRechazoGestionPermisos.DatosInvalidos, rechazo.Tipo);
        Assert.Empty(baseEnMemoria.NuevoContexto().SolicitudesPermiso);
    }
}
