namespace RastreadorHabitos.Modulo.Habitos.DTOs;

// Anulable a propósito: la validación vive en el servicio, con mensajes en español [RD-07].
public class CreacionHabitoDto
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }

    // Días de la semana en que se repite: 1 = lunes ... 7 = domingo.
    public List<int>? Dias { get; set; }
}
