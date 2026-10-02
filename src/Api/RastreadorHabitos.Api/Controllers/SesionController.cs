using Microsoft.AspNetCore.Mvc;
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
}
