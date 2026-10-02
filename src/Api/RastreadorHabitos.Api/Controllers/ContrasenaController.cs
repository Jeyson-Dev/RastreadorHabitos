using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Services;

namespace RastreadorHabitos.Api.Controllers;

// Recuperación y cambio de contraseña. Quién puede invocar cada acción lo declara
// ExigenciasDeRol; los rechazos y los errores los traduce ManejadorErroresGlobal.
[ApiController]
[Route("api/contrasena")]
public class ContrasenaController : ControllerBase
{
    private readonly IContrasenaService _contrasenaService;

    public ContrasenaController(IContrasenaService contrasenaService)
    {
        _contrasenaService = contrasenaService;
    }

    // Misma respuesta exista o no el correo [RF-CA-09].
    [HttpPost("recuperar")]
    public async Task<IActionResult> SolicitarRecuperacion([FromBody] RecuperacionSolicitudDto solicitud)
    {
        await _contrasenaService.SolicitarRecuperacionAsync(solicitud);
        return Ok(new { mensaje = "Si el correo está registrado, recibirá un código para restablecer la contraseña." });
    }

    [HttpPost("restablecer")]
    public async Task<IActionResult> Restablecer([FromBody] RestablecimientoSolicitudDto solicitud)
    {
        await _contrasenaService.RestablecerAsync(solicitud);
        return Ok(new { mensaje = "Contraseña restablecida. Inicie sesión con la contraseña nueva." });
    }
}
