using RastreadorHabitos.Modulo.Habitos.DTOs;

namespace RastreadorHabitos.Modulo.Habitos.Services;

// Métricas que el módulo calcula sobre sus propias tablas y entrega ya agregadas a Reportes
// (flecha «Entrega métricas ya calculadas» del diseño) [RF-NEG-08].
public interface IMetricasHabitos
{
    // ¿Qué porcentaje de los días programados de sus hábitos cumplió cada semana? Una fila por
    // semana entre desde y hasta (ambos incluidos; sin ellos, desde la creación de cada hábito y
    // hasta hoy). Con usuarioId, solo sus hábitos; con null, los de todos.
    Task<IReadOnlyList<CumplimientoSemanalDto>> CumplimientoSemanalAsync(Guid? usuarioId, DateOnly? desde, DateOnly? hasta);
}
