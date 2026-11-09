using Microsoft.AspNetCore.Mvc;

namespace RastreadorHabitos.Api.Controllers;

// Respuesta de la dirección raíz (http://localhost:5003): confirma que la API está en
// funcionamiento al abrirla en el navegador. Quién puede invocarla lo declara ExigenciasDeRol.
[ApiController]
[Route("")]
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
