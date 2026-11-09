using RastreadorHabitos.Core.Documentos.Data;
using RastreadorHabitos.Core.Documentos.DTOs;
using RastreadorHabitos.Core.Documentos.Entities;
using RastreadorHabitos.Core.Documentos.Reglas;

namespace RastreadorHabitos.Core.Documentos.Services;

public class DocumentosService : IDocumentosService
{
    private readonly DocumentosDbContext _context;
    private readonly IAlmacenArchivos _almacen;
    private readonly TimeProvider _reloj;

    public DocumentosService(DocumentosDbContext context, IAlmacenArchivos almacen, TimeProvider reloj)
    {
        _context = context;
        _almacen = almacen;
        _reloj = reloj;
    }

    public async Task<DocumentoDto> SubirAsync(Guid usuarioId, ArchivoSubidoDto? archivo)
    {
        // Archivo ausente o vacío: rechazo controlado, nunca una excepción sin manejar [RD-07].
        if (archivo?.Contenido is null || archivo.Tamano <= 0)
        {
            throw new RechazoDocumentosException(TipoRechazoDocumentos.DatosInvalidos,
                "Adjunte un archivo con contenido en el campo «archivo» (multipart/form-data).");
        }

        var nombre = ReglasArchivo.NormalizarNombre(archivo.Nombre)
            ?? throw new RechazoDocumentosException(TipoRechazoDocumentos.DatosInvalidos,
                $"El archivo debe tener un nombre de hasta {ReglasArchivo.LongitudMaximaNombre} caracteres.");

        var documento = new Documento(nombre, ReglasArchivo.NormalizarTipo(archivo.Tipo), archivo.Tamano,
                                      usuarioId, _reloj.GetUtcNow().UtcDateTime);

        // Primero el archivo y después el registro: nunca queda un registro sin su archivo. Si
        // el registro no se guarda, se borra el archivo para no dejarlo huérfano.
        await _almacen.GuardarAsync(documento.RutaAlmacenamiento, archivo.Contenido);
        try
        {
            _context.Documentos.Add(documento);
            await _context.SaveChangesAsync();
        }
        catch
        {
            _almacen.Borrar(documento.RutaAlmacenamiento);
            throw;
        }

        return DocumentoDto.Desde(documento);
    }
}
