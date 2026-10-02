using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Services;

namespace RastreadorHabitos.Api.Controllers;

// Los rechazos de negocio y los errores inesperados los traduce ManejadorErroresGlobal.
[ApiController]
[Route("api/cuentas")]
public class CuentasController : ControllerBase
{
    private readonly ICuentaService _cuentaService;

    public CuentasController(ICuentaService cuentaService)
    {
        _cuentaService = cuentaService;
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Registrar([FromBody] RegistroSolicitudDto solicitud)
    {
        await _cuentaService.RegistrarAsync(solicitud);
        return StatusCode(StatusCodes.Status201Created,
            new { mensaje = "Registro exitoso. Revise su correo para activar la cuenta." });
    }

    // GET porque se abre directamente desde el enlace del correo.
    [HttpGet("activar")]
    public async Task<IActionResult> Activar([FromQuery] string? token)
    {
        await _cuentaService.ActivarAsync(token);
        return Ok(new { mensaje = "Cuenta activada. Ya puede iniciar sesión." });
    }
}
