namespace RastreadorHabitos.Modulo.Metas.Reglas;

// Quién ejecuta una transición de la meta.
public enum EjecutorTransicion
{
    // El sistema, al consultar la meta: la mueve según su avance y su fecha límite.
    Sistema,

    // El usuario dueño de la meta, con una operación explícita.
    Dueno
}

// Lo que una transición necesita para evaluar su condición.
// Avance: cumplimientos de los hábitos de la meta entre su fecha de inicio y su fecha límite.
// NuevaFechaLimite: solo al reactivar.
public sealed record ContextoTransicion(
    EjecutorTransicion Ejecutor,
    int Avance,
    DateOnly Hoy,
    DateOnly? NuevaFechaLimite = null);
