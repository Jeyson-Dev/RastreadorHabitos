namespace RastreadorHabitos.Modulo.Habitos.Entities;

// Día de la semana en que se repite un hábito: 1 = lunes ... 7 = domingo.
public class HabitoDia
{
    public Guid HabitoId { get; set; }
    public int DiaSemana { get; set; }
}
