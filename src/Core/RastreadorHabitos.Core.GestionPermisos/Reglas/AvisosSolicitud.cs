using RastreadorHabitos.Core.GestionPermisos.Entities;

namespace RastreadorHabitos.Core.GestionPermisos.Reglas;

// Qué avisa Gestión de permisos y con qué texto, en un solo lugar.
public static class AvisosSolicitud
{
    public const string TipoCreada = "SolicitudPermisoCreada";
    public const string TipoAprobada = "SolicitudPermisoAprobada";
    public const string TipoRechazada = "SolicitudPermisoRechazada";

    // Para los Administradores [RF-NOT-01].
    public static string MensajeCreada(SolicitudPermiso solicitud) =>
        $"Hay una solicitud nueva del permiso {solicitud.Permiso} pendiente de resolver (solicitud {solicitud.Id}).";

    // Para el solicitante: el resultado y, si se rechazó, el motivo [RF-NOT-02].
    public static string MensajeAprobada(SolicitudPermiso solicitud) =>
        $"Su solicitud del permiso {solicitud.Permiso} fue aprobada y el permiso ya está aplicado.";

    public static string MensajeRechazada(SolicitudPermiso solicitud) =>
        $"Su solicitud del permiso {solicitud.Permiso} fue rechazada. Motivo: {solicitud.MotivoRechazo}";
}
