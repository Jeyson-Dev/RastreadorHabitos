namespace RastreadorHabitos.Modulo.Habitos.Entities;

// Lo que el usuario registró de un hábito en un día. Hay uno solo por hábito y día.
public class RegistroDiario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HabitoId { get; set; }

    // La asigna el servidor: siempre el día de hoy.
    public DateOnly Fecha { get; set; }

    public bool Cumplido { get; set; }
}
