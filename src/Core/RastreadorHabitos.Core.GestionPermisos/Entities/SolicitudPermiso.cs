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

    // Toda solicitud nace Pendiente [RF-GP-01]. Solo cambia en Aprobar y en CambiarEstado.
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

    // Queda registrado quién aprobó y cuándo [RF-GP-03]. Si la transición no está permitida,
    // no se registra nada.
    public void Aprobar(Guid administradorId, DateTime ahoraUtc)
    {
        RechazarSiEsPropia(administradorId);

        // Solo se aprueba una solicitud Pendiente: aprobar dos veces se rechaza y el estado no
        // cambia [RF-GP-06]. Esta condición decide la aprobación; coincide con Pendiente → Aprobada
        // de TransicionesSolicitud.
        var puedeAprobarse = Estado != EstadoSolicitud.Rechazada;
        if (!puedeAprobarse)
        {
            throw RechazoTransicion(EstadoSolicitud.Aprobada);
        }

        Estado = EstadoSolicitud.Aprobada;
        ResueltaPorId = administradorId;
        FechaResolucionUtc = ahoraUtc;
    }

    // Sin motivo escrito, el rechazo no se completa [RF-GP-04].
    public void Rechazar(Guid administradorId, string? motivo, DateTime ahoraUtc)
    {
        RechazarSiEsPropia(administradorId);
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new RechazoGestionPermisosException(TipoRechazoGestionPermisos.DatosInvalidos,
                "El motivo del rechazo es obligatorio.");
        }

        CambiarEstado(EstadoSolicitud.Rechazada);
        ResueltaPorId = administradorId;
        FechaResolucionUtc = ahoraUtc;
        MotivoRechazo = motivo.Trim();
    }

    // Lo marca el sistema en cuanto el permiso aprobado quedó aplicado [RF-GP-05].
    public void MarcarAplicada()
    {
        CambiarEstado(EstadoSolicitud.Aplicada);
    }

    // Nadie resuelve su propia solicitud [RF-GP-07].
    private void RechazarSiEsPropia(Guid administradorId)
    {
        if (administradorId == SolicitanteId)
        {
            throw new RechazoGestionPermisosException(TipoRechazoGestionPermisos.NoPermitido,
                "Nadie puede resolver su propia solicitud.");
        }
    }

    // Resuelve contra TransicionesSolicitud las transiciones de MarcarAplicada y Rechazar [RD-04].
    // Una transición fuera de la tabla se rechaza y el estado no cambia [RF-GP-06].
    private void CambiarEstado(EstadoSolicitud nuevo)
    {
        if (!TransicionesSolicitud.EsPermitida(Estado, nuevo))
        {
            throw RechazoTransicion(nuevo);
        }

        Estado = nuevo;
    }

    private RechazoGestionPermisosException RechazoTransicion(EstadoSolicitud nuevo) =>
        new(TipoRechazoGestionPermisos.TransicionNoPermitida,
            $"La solicitud está en estado {Estado} y no puede pasar a {nuevo}.");
}
