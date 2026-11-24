using Microsoft.AspNetCore.Mvc;

namespace RastreadorHabitos.Api.Controllers;

// Estado de la API (http://localhost:5003/api/estado): confirma que está en funcionamiento. La
// dirección raíz queda libre para el frontend. Quién puede invocarla lo declara ExigenciasDeRol.
[ApiController]
[Route("api/estado")]
public class InicioController : ControllerBase
{
    [HttpGet]
    public IActionResult Estado()
    {
        return Ok(new
        {
            aplicacion = "Rastreador de hábitos con metas",
            estado = "En funcionamiento",
            api = "/api"
        });
    }
}
