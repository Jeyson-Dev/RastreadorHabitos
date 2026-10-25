using RastreadorHabitos.Core.GestionPermisos.DTOs;

namespace RastreadorHabitos.Tests.GestionPermisos;

// [RF-GP-02] Un Administrador consulta las solicitudes pendientes de todos los usuarios; un
// usuario Estándar no puede ver las solicitudes de otros usuarios, solo las suyas.
public class ConsultaPendientesTests
{
    private static readonly Guid EstandarId = Guid.NewGuid();
    private static readonly Guid OtroUsuarioId = Guid.NewGuid();
    private static readonly Guid AdministradorId = Guid.NewGuid();

    private static Task<SolicitudPermisoDto> CrearAsync(BaseEnMemoria baseEnMemoria, Guid solicitanteId) =>
        baseEnMemoria.NuevoServicio()
            .CrearAsync(solicitanteId, new CreacionSolicitudPermisoDto { Permiso = "Administrador" });

    // [RF-GP-02] El Administrador ve las pendientes de todos los solicitantes.
    [Fact]
    public async Task ListarPendientes_ComoAdministrador_DevuelveLasPendientesDeTodosLosSolicitantes()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var deUno = await CrearAsync(baseEnMemoria, EstandarId);
        var deOtro = await CrearAsync(baseEnMemoria, OtroUsuarioId);

        // Actuar
        var pendientes = await baseEnMemoria.NuevoServicio().ListarPendientesAsync(AdministradorId, esAdministrador: true);

        // Verificar
        Assert.Equal(new[] { deUno.Id, deOtro.Id }.Order(), pendientes.Select(s => s.Id).Order());
    }

    // [RF-GP-02] Las ya resueltas no aparecen entre las pendientes.
    [Fact]
    public async Task ListarPendientes_NoIncluyeLasSolicitudesYaResueltas()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var pendiente = await CrearAsync(baseEnMemoria, EstandarId);
        var resuelta = await CrearAsync(baseEnMemoria, OtroUsuarioId);
        await baseEnMemoria.NuevoServicio().AprobarAsync(resuelta.Id, AdministradorId);

        // Actuar
        var pendientes = await baseEnMemoria.NuevoServicio().ListarPendientesAsync(AdministradorId, esAdministrador: true);

        // Verificar
        Assert.Equal(pendiente.Id, Assert.Single(pendientes).Id);
    }

    // [RF-GP-02] El Estándar ve sus propias solicitudes.
    [Fact]
    public async Task ListarPendientes_ComoEstandar_DevuelveSusPropiasSolicitudes()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        var propia = await CrearAsync(baseEnMemoria, EstandarId);
        await CrearAsync(baseEnMemoria, OtroUsuarioId);

        // Actuar
        var pendientes = await baseEnMemoria.NuevoServicio().ListarPendientesAsync(EstandarId, esAdministrador: false);

        // Verificar
        Assert.Contains(pendientes, s => s.Id == propia.Id);
    }

    // [RF-GP-02] El Estándar no ve las solicitudes de otros usuarios.
    [Fact]
    public async Task ListarPendientes_ComoEstandar_NoDevuelveLasSolicitudesDeOtrosUsuarios()
    {
        // Preparar
        var baseEnMemoria = new BaseEnMemoria();
        await CrearAsync(baseEnMemoria, EstandarId);
        var deOtro = await CrearAsync(baseEnMemoria, OtroUsuarioId);

        // Actuar
        var pendientes = await baseEnMemoria.NuevoServicio().ListarPendientesAsync(EstandarId, esAdministrador: false);

        // Verificar
        Assert.DoesNotContain(pendientes, s => s.Id == deOtro.Id);
    }
}
