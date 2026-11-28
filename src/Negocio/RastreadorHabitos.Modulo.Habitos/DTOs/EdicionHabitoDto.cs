namespace RastreadorHabitos.Modulo.Habitos.DTOs;

// Datos nuevos de un hábito propio: reemplazan nombre, descripción y días. Anulable a propósito:
// la validación vive en el servicio, con mensajes en español [RD-07].
public class EdicionHabitoDto
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }

    // Días de la semana en que se repite: 1 = lunes ... 7 = domingo.
    public List<int>? Dias { get; set; }
}
