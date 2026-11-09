using RastreadorHabitos.Core.Documentos.DTOs;

namespace RastreadorHabitos.Core.Documentos.Services;

// Esta pieza nunca autentica: recibe el Id y el rol del usuario que Control de acceso ya
// identificó. Quién puede invocar cada operación lo declara la tabla única de exigencias de rol
// de la Api [RD-06].
public interface IDocumentosService
{
    // Registra nombre, tipo, tamaño, fecha y quién lo subió [RF-DOC-01]. Un archivo ausente o
    // vacío se rechaza [RD-07].
    Task<DocumentoDto> SubirAsync(Guid usuarioId, ArchivoSubidoDto? archivo);
}
