using RastreadorHabitos.Modulo.Metas.DTOs;

namespace RastreadorHabitos.Modulo.Metas.Services;

// Operaciones de Metas. Siempre sobre las metas del usuario que Control de acceso ya identificó;
// quién puede invocarlas lo declara la tabla única de exigencias de rol de la Api. Antes de
// responder, el sistema aplica las transiciones suyas que correspondan al avance y a la fecha.
public interface IMetasService
{
    // La meta nace Pendiente, con hábitos del propio usuario.
    Task<MetaDto> CrearAsync(Guid usuarioId, CreacionMetaDto meta);

    Task<IReadOnlyList<MetaDto>> ListarAsync(Guid usuarioId);

    Task<MetaDto> ObtenerAsync(Guid usuarioId, Guid metaId);

    // En progreso → Completada, si el avance alcanza el objetivo.
    Task<MetaDto> CompletarAsync(Guid usuarioId, Guid metaId);

    // Fallida → En progreso, con una fecha límite nueva posterior a hoy.
    Task<MetaDto> ReactivarAsync(Guid usuarioId, Guid metaId, ReactivacionMetaDto reactivacion);
}
