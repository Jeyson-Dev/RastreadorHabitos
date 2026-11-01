namespace RastreadorHabitos.Modulo.Metas.DTOs;

// Anulable a propósito: sin la fecha nueva, rechazo controlado [RD-07].
public class ReactivacionMetaDto
{
    public DateOnly? NuevaFechaLimite { get; set; }
}
