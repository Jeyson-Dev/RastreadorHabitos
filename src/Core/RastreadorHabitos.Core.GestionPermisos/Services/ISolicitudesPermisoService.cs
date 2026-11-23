using RastreadorHabitos.Core.GestionPermisos.DTOs;

namespace RastreadorHabitos.Core.GestionPermisos.Services;

// Esta pieza nunca autentica: recibe el Id del usuario que Control de acceso ya identificó.
// Quién puede invocar cada operación lo declara la tabla única de exigencias de rol de la Api.
public interface ISolicitudesPermisoService
{
    // La solicitud nace Pendiente [RF-GP-01].
    Task<SolicitudPermisoDto> CrearAsync(Guid solicitanteId, CreacionSolicitudPermisoDto solicitud);

    // Un Administrador ve las pendientes de todos los usuarios; un Estándar, solo las suyas
    // [RF-GP-02]. El rol lo decide Control de acceso: esta pieza solo filtra según lo que recibe.
    Task<IReadOnlyList<SolicitudPermisoDto>> ListarPendientesAsync(Guid consultanteId, bool esAdministrador);

    // Aprueba y aplica el permiso en la misma operación: la solicitud queda Aplicada y el
    // solicitante ya puede ejecutar la acción que pidió [RF-GP-03, RF-GP-05].
    Task<SolicitudPermisoDto> AprobarAsync(Guid solicitudId, Guid administradorId);

    // Sin motivo escrito el rechazo no se completa [RF-GP-04].
    Task<SolicitudPermisoDto> RechazarAsync(Guid solicitudId, Guid administradorId, RechazoSolicitudPermisoDto rechazo);

    // contarSolicitudesPorEstado: para cada intervalo, las solicitudes creadas en él contadas por
    // estado, en el motor de datos (GROUP BY Estado) [RF-REP-01, RF-REP-03]. Una fila por
    // intervalo, en el mismo orden. Con solicitanteId, solo las de ese usuario [RF-REP-04].
    Task<IReadOnlyList<ConteoSolicitudesDto>> ContarSolicitudesPorEstadoAsync(
        Guid? solicitanteId, IReadOnlyList<PeriodoUtc> periodos);

    // La primera y la última fecha de creación entre desdeUtc (incluido) y hastaUtc (excluido), o
    // null si no hay ninguna: sirve para saber qué intervalos pedir.
    Task<(DateTime PrimeraUtc, DateTime UltimaUtc)?> FechasDeCreacionExtremasAsync(
        Guid? solicitanteId, DateTime? desdeUtc, DateTime? hastaUtc);
}
