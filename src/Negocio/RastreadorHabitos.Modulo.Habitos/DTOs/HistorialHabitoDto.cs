namespace RastreadorHabitos.Modulo.Habitos.DTOs;

// Registros diarios de un hábito en un rango de días locales, con sus rachas calculadas sobre todo
// el historial (no solo sobre el rango).
public class HistorialHabitoDto
{
    public Guid HabitoId { get; set; }
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int RachaActual { get; set; }
    public int MejorRacha { get; set; }
    public List<RegistroDiarioDto> Registros { get; set; } = [];
}
