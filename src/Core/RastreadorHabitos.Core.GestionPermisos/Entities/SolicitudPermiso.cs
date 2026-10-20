namespace RastreadorHabitos.Core.GestionPermisos.Entities;

// Solicitud de permiso de un usuario, resuelta por un Administrador.
public class SolicitudPermiso
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // Quién la pidió: solo el Id que entrega Control de acceso; esta pieza no conoce sus tablas.
    public Guid SolicitanteId { get; private set; }

    // Permiso que pide, uno de Reglas/PermisosSolicitables.
    public string Permiso { get; private set; } = string.Empty;

    // Toda solicitud nace Pendiente [RF-GP-01].
    public EstadoSolicitud Estado { get; private set; } = EstadoSolicitud.Pendiente;

    public DateTime FechaCreacionUtc { get; private set; }

    // Quién la resolvió y cuándo [RF-GP-03], y el motivo si la rechazó [RF-GP-04].
    public Guid? ResueltaPorId { get; private set; }
    public DateTime? FechaResolucionUtc { get; private set; }
    public string? MotivoRechazo { get; private set; }

    // Para EF Core.
    private SolicitudPermiso()
    {
    }

    public SolicitudPermiso(Guid solicitanteId, string permiso, DateTime ahoraUtc)
    {
        SolicitanteId = solicitanteId;
        Permiso = permiso;
        FechaCreacionUtc = ahoraUtc;
    }
}
