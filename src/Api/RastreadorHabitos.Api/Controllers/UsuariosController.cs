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

    // Id del Administrador que hace la petición; la autenticación ya validó su sesión.
    private Guid UsuarioActual() =>
        Guid.Parse(User.FindFirst(OpcionesSesion.ClaimUsuario)!.Value);
}
