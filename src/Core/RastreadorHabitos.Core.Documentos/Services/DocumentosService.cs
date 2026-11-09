using Microsoft.EntityFrameworkCore;
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

    public async Task<IReadOnlyList<DocumentoDto>> ListarAsync(Guid usuarioId, bool esAdministrador)
    {
        // Los eliminados no aparecen para nadie [RF-DOC-04].
        var consulta = _context.Documentos.Where(d => !d.Eliminado);

        // Un Estándar solo ve los suyos, nunca los de otros usuarios [RF-DOC-02].
        if (!esAdministrador)
        {
            consulta = consulta.Where(d => d.SubidoPorId == usuarioId);
        }

        var documentos = await consulta.OrderByDescending(d => d.FechaSubidaUtc).ToListAsync();
        return documentos.Select(DocumentoDto.Desde).ToList();
    }

    public async Task<DescargaDocumentoDto> DescargarAsync(Guid documentoId, Guid usuarioId, bool esAdministrador)
    {
        var documento = await BuscarDelDuenoOAdministradorAsync(documentoId, usuarioId, esAdministrador,
            "Solo el dueño del documento o un Administrador pueden descargarlo.");

        var contenido = _almacen.Abrir(documento.RutaAlmacenamiento)
            ?? throw new RechazoDocumentosException(TipoRechazoDocumentos.NoEncontrado,
                "El archivo de este documento ya no está disponible.");

        return new DescargaDocumentoDto { Documento = DocumentoDto.Desde(documento), Contenido = contenido };
    }

    // Un documento eliminado ya no existe para nadie [RF-DOC-04]. Uno ajeno se rechaza salvo para
    // el Administrador: la regla se decide aquí, en el servidor, se construya como se construya
    // la petición [RF-DOC-03, RD-06].
    private async Task<Documento> BuscarDelDuenoOAdministradorAsync(Guid documentoId, Guid usuarioId,
                                                                   bool esAdministrador, string mensajeAjeno)
    {
        var documento = await _context.Documentos.FirstOrDefaultAsync(d => d.Id == documentoId && !d.Eliminado)
            ?? throw new RechazoDocumentosException(TipoRechazoDocumentos.NoEncontrado,
                "No existe un documento con ese id.");

        if (!esAdministrador && documento.SubidoPorId != usuarioId)
        {
            throw new RechazoDocumentosException(TipoRechazoDocumentos.NoPermitido, mensajeAjeno);
        }

        return documento;
    }
}
