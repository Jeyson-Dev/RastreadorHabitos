using RastreadorHabitos.Core.GestionPermisos.Reglas;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Core.GestionPermisos.Entities;

// Solicitud de permiso de un usuario, resuelta por un Administrador.
public class SolicitudPermiso
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // Quién la pidió: solo el Id que entrega Control de acceso; esta pieza no conoce sus tablas.
    public Guid SolicitanteId { get; private set; }

    // Permiso que pide, uno de Reglas/PermisosSolicitables.
    public string Permiso { get; private set; } = string.Empty;

    // Toda solicitud nace Pendiente [RF-GP-01]. Solo cambia por CambiarEstado.
    public EstadoSolicitud Estado { get; private set; } = TransicionesSolicitud.EstadoInicial;

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

    // Único punto donde cambia el estado: resuelve cada transición contra TransicionesSolicitud
    // [RD-04]. Una transición fuera de la tabla se rechaza y el estado no cambia [RF-GP-06].
    private void CambiarEstado(EstadoSolicitud nuevo)
    {
        if (!TransicionesSolicitud.EsPermitida(Estado, nuevo))
        {
            throw new RechazoGestionPermisosException(TipoRechazoGestionPermisos.TransicionNoPermitida,
                $"La solicitud está en estado {Estado} y no puede pasar a {nuevo}.");
        }

        Estado = nuevo;
    }
}
