namespace RastreadorHabitos.Modulo.Metas.Entities;

// Hábito que aporta a una meta. HabitoId es una referencia lógica, sin clave foránea: Hábitos y
// Metas no comparten tablas.
public class MetaHabito
{
    public Guid MetaId { get; set; }
    public Guid HabitoId { get; set; }
}
