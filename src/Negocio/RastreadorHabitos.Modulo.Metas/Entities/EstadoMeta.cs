namespace RastreadorHabitos.Modulo.Metas.Entities;

// Único lugar donde se declaran los estados de una Meta [RF-NEG-03].
// Las transiciones permitidas entre ellos están en Reglas/TransicionesMeta [RD-04].
public enum EstadoMeta
{
    // Recién creada: todavía no tiene ningún cumplimiento.
    Pendiente,

    // Ya tiene al menos un cumplimiento y no alcanzó el objetivo.
    EnProgreso,

    // Alcanzó el objetivo antes de la fecha límite. Terminal [RF-NEG-05].
    Completada,

    // Terminó la fecha límite sin alcanzar el objetivo. Terminal [RF-NEG-05].
    Fallida
}
