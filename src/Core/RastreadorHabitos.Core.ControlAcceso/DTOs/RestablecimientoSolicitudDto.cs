namespace RastreadorHabitos.Core.ControlAcceso.DTOs;

// Anulables a propósito: la validación vive en el servicio, con mensajes en español [RD-07].
public class RestablecimientoSolicitudDto
{
    public string? Email { get; set; }
    public string? Codigo { get; set; }
    public string? ContrasenaNueva { get; set; }
}
