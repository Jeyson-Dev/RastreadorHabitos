namespace RastreadorHabitos.Modulo.Metas.Entities;

// Único lugar donde se declaran los estados de una Meta [RF-NEG-03].
// Las transiciones permitidas entre ellos están en Reglas/TransicionesMeta [RD-04].
// No usa ni comparte los estados de Gestión de permisos [RF-NEG-09].
public enum EstadoMeta
{
    // Recién creada: todavía no tiene ningún cumplimiento.
    Pendiente,

    // Ya tiene al menos un cumplimiento y su dueño todavía no la cerró.
    EnProgreso,

    // Su dueño la cerró con el avance suficiente. Terminal: de ella no sale ninguna
    // transición [RF-NEG-05].
    Completada,

    // Venció la fecha límite sin alcanzar el objetivo. Su dueño puede reactivarla con una fecha
    // límite nueva.
    Fallida
}
