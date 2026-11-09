using Microsoft.AspNetCore.Mvc;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.Documentos.DTOs;
using RastreadorHabitos.Core.Documentos.Services;

namespace RastreadorHabitos.Api.Controllers;

// Manejador de documentos. Quién puede invocar cada acción lo declara ExigenciasDeRol [RD-06];
// los rechazos y los errores inesperados los traduce ManejadorErroresGlobal.
[ApiController]
[Route("api/documentos")]
public class DocumentosController : ControllerBase
{
    private const string CampoArchivo = "archivo";

    private readonly IDocumentosService _documentos;

    public DocumentosController(IDocumentosService documentos)
    {
        _documentos = documentos;
    }

    // multipart/form-data con el archivo en el campo «archivo». Sin formulario o sin archivo, el
    // servicio lo rechaza con un mensaje controlado [RD-07].
    [HttpPost]
    public async Task<IActionResult> Subir()
    {
        ArchivoSubidoDto? subido = null;
        if (Request.HasFormContentType)
        {
            var formulario = await Request.ReadFormAsync();
            var archivo = formulario.Files.GetFile(CampoArchivo)
                ?? (formulario.Files.Count == 1 ? formulario.Files[0] : null);
            if (archivo is not null)
            {
                subido = new ArchivoSubidoDto
                {
                    Nombre = archivo.FileName,
                    Tipo = archivo.ContentType,
                    Tamano = archivo.Length,
                    Contenido = archivo.OpenReadStream()
                };
            }
        }

        var creado = await _documentos.SubirAsync(UsuarioActual(), subido);
        return StatusCode(StatusCodes.Status201Created, creado);
    }

    // Ninguna ruta recibe el Id de un usuario: el servicio filtra por quien consulta y su rol
    // vigente, así un Estándar no puede pedir los documentos de otros [RF-DOC-02].
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _documentos.ListarAsync(UsuarioActual(), EsAdministrador()));
    }

    [HttpGet("{id:guid}/descarga")]
    public async Task<IActionResult> Descargar(Guid id)
    {
        var descarga = await _documentos.DescargarAsync(id, UsuarioActual(), EsAdministrador());
        return File(descarga.Contenido, descarga.Documento.Tipo, descarga.Documento.Nombre);
    }

    // Id de quien hace la petición, que Control de acceso ya autenticó; esta pieza no autentica.
    private Guid UsuarioActual() =>
        Guid.Parse(User.FindFirst(OpcionesSesion.ClaimUsuario)!.Value);

    // El rol vigente lo puso Control de acceso al validar la sesión; esta pieza solo lo recibe.
    private bool EsAdministrador() => User.IsInRole(Rol.NombreAdministrador);
}
