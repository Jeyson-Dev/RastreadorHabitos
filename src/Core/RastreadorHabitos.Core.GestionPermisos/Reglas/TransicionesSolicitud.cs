using RastreadorHabitos.Core.GestionPermisos.Entities;

namespace RastreadorHabitos.Core.GestionPermisos.Reglas;

// Único lugar donde se declaran las transiciones de una solicitud de permiso [RD-04].
// Solo valen Pendiente → Aprobada → Aplicada y Pendiente → Rechazada; ninguna otra está
// permitida [RF-GP-06]. Aplicada y Rechazada no tienen salida.
public static class TransicionesSolicitud
{
    private static readonly HashSet<(EstadoSolicitud Desde, EstadoSolicitud Hacia)> Permitidas =
    [
        // Un Administrador aprueba una solicitud pendiente [RF-GP-03].
        (EstadoSolicitud.Pendiente, EstadoSolicitud.Aprobada),

        // El sistema aplica el permiso aprobado, sin intervención manual [RF-GP-05].
        (EstadoSolicitud.Aprobada, EstadoSolicitud.Aplicada),

        // Un Administrador rechaza una solicitud pendiente indicando el motivo [RF-GP-04].
        (EstadoSolicitud.Pendiente, EstadoSolicitud.Rechazada),
        (EstadoSolicitud.Rechazada, EstadoSolicitud.Aplicada),
    ];

    public const EstadoSolicitud EstadoInicial = EstadoSolicitud.Pendiente;

    public static bool EsPermitida(EstadoSolicitud desde, EstadoSolicitud hacia) =>
        Permitidas.Contains((desde, hacia));
}
