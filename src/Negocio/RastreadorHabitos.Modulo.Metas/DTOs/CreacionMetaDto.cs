namespace RastreadorHabitos.Modulo.Metas.DTOs;

// Anulable a propósito: la validación vive en el servicio, con mensajes en español [RD-07].
public class CreacionMetaDto
{
    public string? Nombre { get; set; }
    public int? ObjetivoCumplimientos { get; set; }
    public DateOnly? FechaLimite { get; set; }
    public List<Guid>? HabitoIds { get; set; }
}
