using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Habitos.Services;

namespace RastreadorHabitos.Api.Controllers;

// Hábitos del usuario de la sesión. Quién puede invocar cada acción lo declara ExigenciasDeRol
// [RF-NEG-06]; los rechazos de negocio y los errores inesperados los traduce ManejadorErroresGlobal.
[ApiController]
[Route("api/habitos")]
public class HabitosController : ControllerBase
{
    private readonly IHabitosService _habitos;
    private readonly IPlanDeHoyService _planDeHoy;

    public HabitosController(IHabitosService habitos, IPlanDeHoyService planDeHoy)
    {
        _habitos = habitos;
        _planDeHoy = planDeHoy;
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CreacionHabitoDto habito)
    {
        var creado = await _habitos.CrearAsync(UsuarioActual(), habito);
        return StatusCode(StatusCodes.Status201Created, creado);
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _habitos.ListarAsync(UsuarioActual()));
    }

    [HttpPost("{id:guid}/registros")]
    public async Task<IActionResult> RegistrarCumplimiento(Guid id, [FromBody] RegistroCumplimientoDto registro)
    {
        return Ok(await _habitos.RegistrarCumplimientoAsync(UsuarioActual(), id, registro));
    }

    // Hábitos programados para hoy con el pronóstico de la ciudad indicada. Si el servicio de clima
    // no está disponible, se rechaza de forma controlada y el resto del módulo sigue funcionando.
    [HttpGet("hoy")]
    public async Task<IActionResult> PlanDeHoy([FromQuery] string? ciudad, CancellationToken cancelacion)
    {
        return Ok(await _planDeHoy.ObtenerAsync(UsuarioActual(), ciudad, cancelacion));
    }

    // Id de quien hace la petición, que Control de acceso ya autenticó; el negocio no autentica.
    private Guid UsuarioActual() =>
        Guid.Parse(User.FindFirst(OpcionesSesion.ClaimUsuario)!.Value);
}
