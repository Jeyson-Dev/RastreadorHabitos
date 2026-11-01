using RastreadorHabitos.Modulo.Metas.Entities;
using RastreadorHabitos.Modulo.Metas.Services;

namespace RastreadorHabitos.Modulo.Metas.Reglas;

// Único lugar donde se declaran y se resuelven las transiciones de estado de una Meta [RD-04].
// Es la misma tabla de docs/maquina-de-estados.md, en el mismo orden: desde, hacia, quién la
// ejecuta y condición. Agregar una transición es agregar una fila aquí.
public static class TransicionesMeta
{
    public const EstadoMeta EstadoInicial = EstadoMeta.Pendiente;

    private sealed record Transicion(
        EstadoMeta Desde,
        EstadoMeta Hacia,
        EjecutorTransicion Ejecutor,
        Func<Meta, ContextoTransicion, bool> Condicion,
        string CondicionIncumplida);

    // Transiciones permitidas. Ninguna parte de Completada, el estado terminal [RF-NEG-05].
    private static readonly Transicion[] Permitidas =
    [
        new(EstadoMeta.Pendiente, EstadoMeta.EnProgreso, EjecutorTransicion.Sistema,
            (meta, contexto) => true,
            "La meta todavía no tiene ningún cumplimiento."),

        new(EstadoMeta.EnProgreso, EstadoMeta.Completada, EjecutorTransicion.Dueno,
            (meta, contexto) => contexto.Avance >= meta.ObjetivoCumplimientos,
            "El avance todavía no alcanza el objetivo de la meta."),

        new(EstadoMeta.EnProgreso, EstadoMeta.Fallida, EjecutorTransicion.Sistema,
            (meta, contexto) => contexto.Hoy > meta.FechaLimite && contexto.Avance < meta.ObjetivoCumplimientos,
            "La meta solo falla cuando vence su fecha límite sin alcanzar el objetivo."),

        new(EstadoMeta.Pendiente, EstadoMeta.Fallida, EjecutorTransicion.Sistema,
            (meta, contexto) => contexto.Hoy > meta.FechaLimite && contexto.Avance < meta.ObjetivoCumplimientos,
            "La meta solo falla cuando vence su fecha límite sin alcanzar el objetivo."),

        new(EstadoMeta.Fallida, EstadoMeta.EnProgreso, EjecutorTransicion.Dueno,
            (meta, contexto) => contexto.NuevaFechaLimite > contexto.Hoy,
            "La nueva fecha límite debe ser posterior a hoy."),
    ];

    // Transiciones prohibidas de forma explícita [RF-NEG-04], con el motivo que recibe quien
    // las intenta. Son todas las parejas que no están en Permitidas.
    private static readonly Dictionary<(EstadoMeta Desde, EstadoMeta Hacia), string> Prohibidas = new()
    {
        [(EstadoMeta.Pendiente, EstadoMeta.Completada)] = "Una meta Pendiente no puede completarse: primero debe pasar por En progreso.",
        [(EstadoMeta.EnProgreso, EstadoMeta.Pendiente)] = "El avance de una meta no se deshace: no vuelve a Pendiente.",
        [(EstadoMeta.Completada, EstadoMeta.Pendiente)] = "Una meta Completada es definitiva y no cambia de estado.",
        [(EstadoMeta.Completada, EstadoMeta.EnProgreso)] = "Una meta Completada es definitiva y no cambia de estado.",
        [(EstadoMeta.Completada, EstadoMeta.Fallida)] = "Una meta Completada es definitiva y no cambia de estado.",
        [(EstadoMeta.Fallida, EstadoMeta.Completada)] = "Una meta Fallida no puede completarse: primero debe reactivarse.",
        [(EstadoMeta.Fallida, EstadoMeta.Pendiente)] = "Una meta Fallida se reactiva a En progreso, no a Pendiente.",
    };

    // Resuelve una transición: la rechaza si no está permitida, si no la ejecuta quien le
    // corresponde o si no se cumple su condición. Si la rechaza, el estado no cambia.
    public static void Validar(Meta meta, EstadoMeta hacia, ContextoTransicion contexto)
    {
        var transicion = Permitidas.FirstOrDefault(t => t.Desde == meta.Estado && t.Hacia == hacia)
            ?? throw Rechazo(Prohibidas.GetValueOrDefault((meta.Estado, hacia),
                $"La meta está en estado {meta.Estado} y no puede pasar a {hacia}."));

        if (transicion.Ejecutor != contexto.Ejecutor)
        {
            throw Rechazo(transicion.Ejecutor == EjecutorTransicion.Sistema
                ? "Esa transición la ejecuta el sistema, no el dueño de la meta."
                : "Esa transición la ejecuta el dueño de la meta, no el sistema.");
        }

        if (!transicion.Condicion(meta, contexto))
        {
            throw Rechazo(transicion.CondicionIncumplida);
        }
    }

    // Primera transición del sistema que sale del estado actual y cuya condición se cumple.
    public static EstadoMeta? SiguienteDelSistema(Meta meta, ContextoTransicion contexto) =>
        Permitidas.FirstOrDefault(t => t.Desde == meta.Estado
                                       && t.Ejecutor == EjecutorTransicion.Sistema
                                       && t.Condicion(meta, contexto))?.Hacia;

    public static bool EsPermitida(EstadoMeta desde, EstadoMeta hacia) =>
        Permitidas.Any(t => t.Desde == desde && t.Hacia == hacia);

    // Un estado es terminal si ninguna transición permitida sale de él [RF-NEG-05].
    public static bool EsTerminal(EstadoMeta estado) => Permitidas.All(t => t.Desde != estado);

    private static RechazoMetasException Rechazo(string mensaje) =>
        new(TipoRechazoMetas.TransicionNoPermitida, mensaje);
}
