using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.Auditoria.Services;

namespace RastreadorHabitos.Api.Controllers;

// Historial de auditoría, reservado al Administrador en ExigenciasDeRol [RF-AUD-07, RD-06]: un
// Estándar recibe 403 aunque construya la petición a mano. Solo consulta: no hay ninguna ruta para
// editar ni borrar registros [RF-AUD-06].
[ApiController]
[Route("api/auditoria")]
public class AuditoriaController : ControllerBase
{
    private readonly IHistorialAuditoria _historial;

    public AuditoriaController(IHistorialAuditoria historial)
    {
        _historial = historial;
    }

    // ?usuarioId=&entidad=&desde=AAAA-MM-DD&hasta=AAAA-MM-DD, todos opcionales.
    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] string? usuarioId, [FromQuery] string? entidad,
                                               [FromQuery] string? desde, [FromQuery] string? hasta)
    {
        return Ok(await _historial.ConsultarAsync(usuarioId, entidad, desde, hasta));
    }
}
