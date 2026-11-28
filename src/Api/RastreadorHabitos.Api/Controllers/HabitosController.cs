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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EdicionHabitoDto edicion)
    {
        return Ok(await _habitos.EditarAsync(UsuarioActual(), id, edicion));
    }

    [HttpPost("{id:guid}/archivar")]
    public async Task<IActionResult> Archivar(Guid id)
    {
        return Ok(await _habitos.ArchivarAsync(UsuarioActual(), id));
    }

    [HttpPost("{id:guid}/restaurar")]
    public async Task<IActionResult> Restaurar(Guid id)
    {
        return Ok(await _habitos.RestaurarAsync(UsuarioActual(), id));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        await _habitos.EliminarAsync(UsuarioActual(), id);
        return NoContent();
    }

    [HttpPost("{id:guid}/registros")]
    public async Task<IActionResult> RegistrarCumplimiento(Guid id, [FromBody] RegistroCumplimientoDto registro)
    {
        return Ok(await _habitos.RegistrarCumplimientoAsync(UsuarioActual(), id, registro));
    }

    // Solo lectura: historial de registros y rachas de un hábito propio.
    [HttpGet("{id:guid}/registros")]
    public async Task<IActionResult> ConsultarRegistros(Guid id, [FromQuery] string? desde, [FromQuery] string? hasta)
    {
        return Ok(await _habitos.ConsultarRegistrosAsync(UsuarioActual(), id, desde, hasta));
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
