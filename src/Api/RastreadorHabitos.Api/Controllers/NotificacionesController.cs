using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.Notificaciones.DTOs;
using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Api.Controllers;

// Notificaciones del usuario de la sesión. Quién puede invocar cada acción lo declara
// ExigenciasDeRol [RD-06]; los rechazos y los errores inesperados los traduce
// ManejadorErroresGlobal. Ninguna ruta recibe el Id de un usuario: siempre es el de la sesión.
[ApiController]
[Route("api/notificaciones")]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionesService _notificaciones;

    public NotificacionesController(INotificacionesService notificaciones)
    {
        _notificaciones = notificaciones;
    }

    // ?leida=true o ?leida=false para ver solo las leídas o solo las no leídas [RF-NOT-04].
    [HttpGet]
    public async Task<IActionResult> ListarPropias([FromQuery] bool? leida)
    {
        return Ok(await _notificaciones.ListarPropiasAsync(UsuarioActual(), leida));
    }

    [HttpPost("{id:guid}/leida")]
    public async Task<IActionResult> MarcarLeida(Guid id)
    {
        return Ok(await _notificaciones.MarcarLeidaAsync(UsuarioActual(), id));
    }

    [HttpGet("preferencias")]
    public async Task<IActionResult> ObtenerPreferencias()
    {
        return Ok(await _notificaciones.ObtenerPreferenciasAsync(UsuarioActual()));
    }

    [HttpPut("preferencias")]
    public async Task<IActionResult> ActualizarPreferencias([FromBody] ActualizacionPreferenciasDto preferencias)
    {
        return Ok(await _notificaciones.ActualizarPreferenciasAsync(UsuarioActual(), preferencias));
    }

    // Id de quien hace la petición, que Control de acceso ya autenticó.
    private Guid UsuarioActual() =>
        Guid.Parse(User.FindFirst(OpcionesSesion.ClaimUsuario)!.Value);
}
