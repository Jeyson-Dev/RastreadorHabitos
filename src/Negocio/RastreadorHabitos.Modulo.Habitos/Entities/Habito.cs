namespace RastreadorHabitos.Modulo.Habitos.Entities;

// Hábito de un usuario, con los días de la semana en que está programado.
public class Habito
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Dueño del hábito: solo el Id que entrega Control de acceso; el negocio no conoce sus tablas.
    public Guid UsuarioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;

    // null mientras está activo. Archivado conserva su historial y sus rachas, no cuenta para el
    // límite y no se registra (regla 3 del diseño).
    public DateTime? FechaArchivadoUtc { get; set; }

    public bool Archivado => FechaArchivadoUtc is not null;

    public List<HabitoDia> Dias { get; set; } = [];
    public List<RegistroDiario> Registros { get; set; } = [];
}
