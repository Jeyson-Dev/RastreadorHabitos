namespace RastreadorHabitos.Core.Documentos.DTOs;

// El registro del documento y su contenido, para responder la descarga [RF-DOC-03].
public class DescargaDocumentoDto
{
    public required DocumentoDto Documento { get; init; }
    public required Stream Contenido { get; init; }
}
