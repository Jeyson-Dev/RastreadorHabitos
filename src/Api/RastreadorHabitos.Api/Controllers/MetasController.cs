using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Modulo.Metas.DTOs;
using RastreadorHabitos.Modulo.Metas.Services;

namespace RastreadorHabitos.Api.Controllers;

// Metas del usuario de la sesión. Quién puede invocar cada acción lo declara ExigenciasDeRol
// [RF-NEG-06]; los rechazos de negocio y los errores inesperados los traduce ManejadorErroresGlobal.
[ApiController]
[Route("api/metas")]
public class MetasController : ControllerBase
{
    private readonly IMetasService _metas;

    public MetasController(IMetasService metas)
    {
        _metas = metas;
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CreacionMetaDto meta)
    {
        var creada = await _metas.CrearAsync(UsuarioActual(), meta);
        return StatusCode(StatusCodes.Status201Created, creada);
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _metas.ListarAsync(UsuarioActual()));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id)
    {
        return Ok(await _metas.ObtenerAsync(UsuarioActual(), id));
    }

    [HttpPost("{id:guid}/completar")]
    public async Task<IActionResult> Completar(Guid id)
    {
        return Ok(await _metas.CompletarAsync(UsuarioActual(), id));
    }

    [HttpPost("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar(Guid id, [FromBody] ReactivacionMetaDto reactivacion)
    {
        return Ok(await _metas.ReactivarAsync(UsuarioActual(), id, reactivacion));
    }

    // Id de quien hace la petición, que Control de acceso ya autenticó; el negocio no autentica.
    private Guid UsuarioActual() =>
        Guid.Parse(User.FindFirst(OpcionesSesion.ClaimUsuario)!.Value);
}
