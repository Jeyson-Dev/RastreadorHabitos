using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.Reportes.Services;

namespace RastreadorHabitos.Api.Controllers;

// Reportes con agregación. Quién puede invocar cada acción lo declara ExigenciasDeRol [RD-06]; el
// alcance de los datos lo decide la pieza con el Id y el rol vigente de la sesión, nunca un
// parámetro de la petición [RF-REP-04].
[ApiController]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly IReportesService _reportes;

    public ReportesController(IReportesService reportes)
    {
        _reportes = reportes;
    }

    // Reporte del Core [RF-REP-01].
    [HttpGet("solicitudes-por-estado")]
    public Task<IActionResult> SolicitudesPorEstado([FromQuery] string? desde, [FromQuery] string? hasta) =>
        GenerarAsync("solicitudes-por-estado", desde, hasta);

    // Reporte del negocio [RF-REP-02].
    [HttpGet("cumplimiento-semanal")]
    public Task<IActionResult> CumplimientoSemanal([FromQuery] string? desde, [FromQuery] string? hasta) =>
        GenerarAsync("cumplimiento-semanal", desde, hasta);

    private async Task<IActionResult> GenerarAsync(string reporte, string? desde, string? hasta)
    {
        var usuario = Guid.Parse(User.FindFirst(OpcionesSesion.ClaimUsuario)!.Value);
        return Ok(await _reportes.GenerarAsync(reporte, usuario, User.IsInRole(Rol.NombreAdministrador), desde, hasta));
    }
}
