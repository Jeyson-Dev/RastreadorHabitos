namespace RastreadorHabitos.Core.GestionPermisos.Reglas;

// Qué audita Gestión de permisos, en un solo lugar: toda resolución de una solicitud, con quién la
// aprobó o rechazó, qué y cuándo [RF-AUD-02].
public static class EventosAuditoria
{
    public const string Entidad = "SolicitudPermiso";

    public const string SolicitudAprobada = "SolicitudAprobada";
    public const string SolicitudRechazada = "SolicitudRechazada";
}
