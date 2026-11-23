namespace RastreadorHabitos.Modulo.Habitos.DTOs;

// Métrica del módulo: cuánto se cumplió en una semana de lunes a domingo [RF-NEG-08, RF-REP-02].
public class CumplimientoSemanalDto
{
    // El lunes de la semana.
    public DateOnly Semana { get; set; }

    // Días en que los hábitos estaban programados (desde su creación y hasta hoy).
    public int DiasProgramados { get; set; }

    // Días programados con el registro diario «cumplido».
    public int DiasCumplidos { get; set; }

    // DiasCumplidos sobre DiasProgramados, en porcentaje con un decimal.
    public decimal PorcentajeCumplimiento { get; set; }
}
