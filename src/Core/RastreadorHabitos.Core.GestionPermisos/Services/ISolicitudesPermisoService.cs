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
}
