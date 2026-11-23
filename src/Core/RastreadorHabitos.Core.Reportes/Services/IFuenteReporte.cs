using RastreadorHabitos.Core.Reportes.Reglas;

namespace RastreadorHabitos.Core.Reportes.Services;

// Puerto de entrada de métricas: cada pieza o módulo calcula las suyas, ya agrupadas en su propio
// motor de datos, y Reportes solo las recibe, decide el alcance por rol y las entrega. Así
// Reportes nunca conoce las entidades de nadie [RD-03]. La Api registra una fuente por reporte.
public interface IFuenteReporte
{
    // Identificador del reporte en la ruta, por ejemplo «solicitudes-por-estado».
    string Nombre { get; }

    // La pregunta que responde el reporte [RF-REP-02].
    string Pregunta { get; }

    // Filas ya agregadas (menos filas que los registros que resumen) [RF-REP-03]. Con usuarioId,
    // solo los datos de ese usuario; con null, los de todos [RF-REP-04].
    Task<IReadOnlyList<FilaReporte>> CalcularAsync(Guid? usuarioId, RangoFechas rango, TimeZoneInfo zona);
}

// Una fila agregada: el grupo (un mes, una semana) y sus valores (totales, porcentajes).
public sealed record FilaReporte(string Grupo, IReadOnlyDictionary<string, decimal> Valores);
