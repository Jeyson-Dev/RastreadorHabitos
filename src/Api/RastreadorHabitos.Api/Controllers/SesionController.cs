using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Services;

namespace RastreadorHabitos.Api.Controllers;

// Los rechazos de negocio y los errores inesperados los traduce ManejadorErroresGlobal.
[ApiController]
[Route("api/sesion")]
public class SesionController : ControllerBase
{
    private readonly ISesionService _sesionService;

    public SesionController(ISesionService sesionService)
    {
        _sesionService = sesionService;
    }

    [HttpPost("iniciar")]
    public async Task<IActionResult> Iniciar([FromBody] InicioSesionSolicitudDto solicitud)
    {
        var credencial = await _sesionService.IniciarAsync(solicitud);
        return Ok(credencial);
    }

    // Exige sesión (ver ExigenciasDeRol): sin sesión válida se responde 401 antes de llegar aquí [RF-CA-07].
    [HttpGet("usuario")]
    public async Task<IActionResult> UsuarioAutenticado()
    {
        var usuario = await _sesionService.ObtenerUsuarioAutenticadoAsync(SesionActual());
        return Ok(usuario);
    }

    // Después de cerrar, la misma credencial responde 401 [RF-CA-18].
    [HttpPost("cerrar")]
    public async Task<IActionResult> Cerrar()
    {
        await _sesionService.CerrarAsync(SesionActual());
        return Ok(new { mensaje = "Sesión cerrada." });
    }

    // Id de la sesión que viaja en la credencial; la autenticación ya verificó que está abierta.
    private Guid SesionActual() =>
        Guid.Parse(User.FindFirst(OpcionesSesion.ClaimSesion)!.Value);
}
