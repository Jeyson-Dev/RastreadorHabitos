using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Api.Controllers;

// Consulta de la cola de correos, reservada al Administrador en ExigenciasDeRol [RF-NOT-11,
// RD-06]: un Estándar recibe 403 aunque construya la petición a mano.
[ApiController]
[Route("api/correos")]
public class CorreosController : ControllerBase
{
    private readonly IConsultaColaCorreos _cola;

    public CorreosController(IConsultaColaCorreos cola)
    {
        _cola = cola;
    }

    // ?estado=Pendiente, Enviado o Fallido para ver solo esos.
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? estado)
    {
        return Ok(await _cola.ListarAsync(estado));
    }
}
