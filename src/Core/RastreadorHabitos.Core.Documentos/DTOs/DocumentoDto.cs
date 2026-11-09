using RastreadorHabitos.Core.Documentos.Entities;

namespace RastreadorHabitos.Core.Documentos.DTOs;

// Lo consultable de un documento [RF-DOC-01]. No expone dónde está guardado el archivo [RD-08].
public class DocumentoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public DateTime FechaSubidaUtc { get; set; }
    public Guid SubidoPorId { get; set; }

    public static DocumentoDto Desde(Documento documento) => new()
    {
        Id = documento.Id,
        Nombre = documento.Nombre,
        Tipo = documento.Tipo,
        TamanoBytes = documento.TamanoBytes,
        FechaSubidaUtc = documento.FechaSubidaUtc,
        SubidoPorId = documento.SubidoPorId
    };
}
