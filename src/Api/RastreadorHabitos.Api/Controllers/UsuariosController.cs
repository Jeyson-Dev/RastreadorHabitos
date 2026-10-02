using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Services;

namespace RastreadorHabitos.Api.Controllers;

// Administración de usuarios. Quién puede invocar cada acción lo declara ExigenciasDeRol;
// los rechazos de negocio y los errores inesperados los traduce ManejadorErroresGlobal.
[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IAdministracionUsuariosService _administracion;

    public UsuariosController(IAdministracionUsuariosService administracion)
    {
        _administracion = administracion;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _administracion.ListarAsync());
    }

    [HttpPut("{id:guid}/rol")]
    public async Task<IActionResult> CambiarRol(Guid id, [FromBody] CambioRolSolicitudDto solicitud)
    {
        await _administracion.CambiarRolAsync(id, UsuarioActual(), solicitud);
        return Ok(new { mensaje = "Rol actualizado." });
    }

    [HttpPost("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        await _administracion.DesactivarAsync(id, UsuarioActual());
        return Ok(new { mensaje = "Usuario desactivado. Sus sesiones abiertas se cerraron." });
    }

    [HttpPost("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar(Guid id)
    {
        await _administracion.ReactivarAsync(id);
        return Ok(new { mensaje = "Usuario reactivado." });
    }

    // Id del Administrador que hace la petición; la autenticación ya validó su sesión.
    private Guid UsuarioActual() =>
        Guid.Parse(User.FindFirst(OpcionesSesion.ClaimUsuario)!.Value);
}
