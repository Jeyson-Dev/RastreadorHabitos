using RastreadorHabitos.Core.GestionPermisos.DTOs;

namespace RastreadorHabitos.Core.GestionPermisos.Services;

// Esta pieza nunca autentica: recibe el Id del usuario que Control de acceso ya identificó.
// Quién puede invocar cada operación lo declara la tabla única de exigencias de rol de la Api.
public interface ISolicitudesPermisoService
{
    // La solicitud nace Pendiente [RF-GP-01].
    Task<SolicitudPermisoDto> CrearAsync(Guid solicitanteId, CreacionSolicitudPermisoDto solicitud);

    // Solicitudes pendientes de todos los usuarios, reservado al Administrador [RF-GP-02].
    Task<IReadOnlyList<SolicitudPermisoDto>> ListarPendientesAsync();

    // Aprueba y aplica el permiso en la misma operación: la solicitud queda Aplicada y el
    // solicitante ya puede ejecutar la acción que pidió [RF-GP-03, RF-GP-05].
    Task<SolicitudPermisoDto> AprobarAsync(Guid solicitudId, Guid administradorId);

    // Sin motivo escrito el rechazo no se completa [RF-GP-04].
    Task<SolicitudPermisoDto> RechazarAsync(Guid solicitudId, Guid administradorId, RechazoSolicitudPermisoDto rechazo);
}
