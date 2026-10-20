using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Api.Controllers;

// Gestión de permisos. Quién puede invocar cada acción lo declara ExigenciasDeRol [RD-06];
// los rechazos de negocio y los errores inesperados los traduce ManejadorErroresGlobal.
[ApiController]
[Route("api/solicitudes-permiso")]
public class SolicitudesPermisoController : ControllerBase
{
    private readonly ISolicitudesPermisoService _solicitudes;

    public SolicitudesPermisoController(ISolicitudesPermisoService solicitudes)
    {
        _solicitudes = solicitudes;
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CreacionSolicitudPermisoDto solicitud)
    {
        var creada = await _solicitudes.CrearAsync(UsuarioActual(), solicitud);
        return StatusCode(StatusCodes.Status201Created, creada);
    }

    // Ninguna ruta recibe el Id de un solicitante: un Estándar no puede pedir las de otros [RF-GP-02].
    [HttpGet("pendientes")]
    public async Task<IActionResult> ListarPendientes()
    {
        return Ok(await _solicitudes.ListarPendientesAsync());
    }

    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Aprobar(Guid id)
    {
        return Ok(await _solicitudes.AprobarAsync(id, UsuarioActual()));
    }

    [HttpPost("{id:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid id, [FromBody] RechazoSolicitudPermisoDto rechazo)
    {
        return Ok(await _solicitudes.RechazarAsync(id, UsuarioActual(), rechazo));
    }

    // Id de quien hace la petición, que Control de acceso ya autenticó; esta pieza no autentica.
    private Guid UsuarioActual() =>
        Guid.Parse(User.FindFirst(OpcionesSesion.ClaimUsuario)!.Value);
}
