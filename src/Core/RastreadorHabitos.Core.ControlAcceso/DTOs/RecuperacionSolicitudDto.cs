namespace RastreadorHabitos.Core.ControlAcceso.DTOs;

// Anulable a propósito: la validación vive en el servicio, con mensajes en español [RD-07].
public class RecuperacionSolicitudDto
{
    public string? Email { get; set; }
}
