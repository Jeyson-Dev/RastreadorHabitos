using RastreadorHabitos.Modulo.Metas.Entities;

namespace RastreadorHabitos.Modulo.Metas.Reglas;

// Único lugar donde se declaran las transiciones de estado de una Meta [RD-04].
// La tabla completa (desde, hacia, quién la ejecuta, condición) está en docs/maquina-de-estados.md.
public static class TransicionesMeta
{
    // Transiciones permitidas. Todas las ejecuta el sistema, nunca el usuario a mano.
    private static readonly HashSet<(EstadoMeta Desde, EstadoMeta Hacia)> Permitidas =
    [
        // Se registra el primer cumplimiento de uno de sus hábitos, antes de la fecha límite.
        (EstadoMeta.Pendiente, EstadoMeta.EnProgreso),

        // Los cumplimientos alcanzan el objetivo antes de que termine el día de la fecha límite.
        (EstadoMeta.EnProgreso, EstadoMeta.Completada),

        // Termina el día de la fecha límite sin alcanzar el objetivo.
        (EstadoMeta.EnProgreso, EstadoMeta.Fallida),

        // Termina el día de la fecha límite sin ningún cumplimiento.
        (EstadoMeta.Pendiente, EstadoMeta.Fallida),
    ];

    // Prohibidas explícitas [RF-NEG-04]. Ya quedan fuera de Permitidas; se declaran también
    // aquí para que la regla se pueda leer sin deducirla.
    private static readonly HashSet<(EstadoMeta Desde, EstadoMeta Hacia)> ProhibidasExplicitas =
    [
        // Toda meta lograda pasa por En progreso.
        (EstadoMeta.Pendiente, EstadoMeta.Completada),

        // El avance no se deshace.
        (EstadoMeta.EnProgreso, EstadoMeta.Pendiente),

        // Una meta terminada no se reabre.
        (EstadoMeta.Completada, EstadoMeta.EnProgreso),
        (EstadoMeta.Fallida, EstadoMeta.EnProgreso),

        // El resultado de una meta terminada no cambia.
        (EstadoMeta.Completada, EstadoMeta.Fallida),
        (EstadoMeta.Fallida, EstadoMeta.Completada),
    ];

    // Estados terminales: de ellos no sale ninguna transición [RF-NEG-05].
    private static readonly HashSet<EstadoMeta> Terminales = [EstadoMeta.Completada, EstadoMeta.Fallida];

    public const EstadoMeta EstadoInicial = EstadoMeta.Pendiente;

    public static bool EsPermitida(EstadoMeta desde, EstadoMeta hacia) =>
        !Terminales.Contains(desde)
        && !ProhibidasExplicitas.Contains((desde, hacia))
        && Permitidas.Contains((desde, hacia));

    public static bool EsTerminal(EstadoMeta estado) => Terminales.Contains(estado);
}
