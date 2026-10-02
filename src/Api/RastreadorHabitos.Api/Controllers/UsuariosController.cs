using Microsoft.AspNetCore.Mvc;
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
}
