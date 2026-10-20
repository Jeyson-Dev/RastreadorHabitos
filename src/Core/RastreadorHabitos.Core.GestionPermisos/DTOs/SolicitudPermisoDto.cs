using RastreadorHabitos.Core.GestionPermisos.Entities;

namespace RastreadorHabitos.Core.GestionPermisos.DTOs;

public class SolicitudPermisoDto
{
    public Guid Id { get; set; }
    public Guid SolicitanteId { get; set; }
    public string Permiso { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
    public Guid? ResueltaPorId { get; set; }
    public DateTime? FechaResolucionUtc { get; set; }
    public string? MotivoRechazo { get; set; }

    public static SolicitudPermisoDto Desde(SolicitudPermiso solicitud) => new()
    {
        Id = solicitud.Id,
        SolicitanteId = solicitud.SolicitanteId,
        Permiso = solicitud.Permiso,
        Estado = solicitud.Estado.ToString(),
        FechaCreacionUtc = solicitud.FechaCreacionUtc,
        ResueltaPorId = solicitud.ResueltaPorId,
        FechaResolucionUtc = solicitud.FechaResolucionUtc,
        MotivoRechazo = solicitud.MotivoRechazo
    };
}
