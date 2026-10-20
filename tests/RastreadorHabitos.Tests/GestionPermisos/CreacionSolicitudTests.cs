using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Tests.GestionPermisos;

// [RF-GP-01] Un usuario Estándar crea una solicitud de permiso: nace en estado Pendiente.
// [RF-GP-02] Un Administrador consulta las solicitudes pendientes.
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

    [Fact]
    public async Task ListarPendientes_DevuelveLasSolicitudesPendientesDeTodosLosSolicitantes()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var servicio = baseEnMemoria.NuevoServicio();
        var deUno = await servicio.CrearAsync(Guid.NewGuid(), new CreacionSolicitudPermisoDto { Permiso = "Administrador" });
        var deOtro = await servicio.CrearAsync(Guid.NewGuid(), new CreacionSolicitudPermisoDto { Permiso = "Administrador" });

        // Actuar
        var pendientes = await baseEnMemoria.NuevoServicio().ListarPendientesAsync();

        // Verificar
        Assert.Equal(new[] { deUno.Id, deOtro.Id }.Order(), pendientes.Select(s => s.Id).Order());
    }

    [Fact]
    public async Task ListarPendientes_NoIncluyeLasSolicitudesYaResueltas()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var servicio = baseEnMemoria.NuevoServicio();
        var pendiente = await servicio.CrearAsync(Guid.NewGuid(), new CreacionSolicitudPermisoDto { Permiso = "Administrador" });
        var resuelta = await servicio.CrearAsync(Guid.NewGuid(), new CreacionSolicitudPermisoDto { Permiso = "Administrador" });
        await servicio.AprobarAsync(resuelta.Id, Guid.NewGuid());

        // Actuar
        var pendientes = await baseEnMemoria.NuevoServicio().ListarPendientesAsync();

        // Verificar
        Assert.Equal(pendiente.Id, Assert.Single(pendientes).Id);
    }
}
