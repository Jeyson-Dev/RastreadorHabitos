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

    // Nunca incluye documentos eliminados. Un Estándar ve solo los suyos; un Administrador, los
    // de todos los usuarios [RF-DOC-02].
    Task<IReadOnlyList<DocumentoDto>> ListarAsync(Guid usuarioId, bool esAdministrador);

    // Solo el dueño o un Administrador lo descargan; cualquier otro usuario recibe un rechazo
    // [RF-DOC-03].
    Task<DescargaDocumentoDto> DescargarAsync(Guid documentoId, Guid usuarioId, bool esAdministrador);

    // Borrado lógico: deja de aparecer en el listado, pero el registro y el archivo siguen en el
    // almacenamiento [RF-DOC-04]. Solo el dueño o un Administrador lo eliminan.
    Task EliminarAsync(Guid documentoId, Guid usuarioId, bool esAdministrador);
}
