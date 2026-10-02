namespace RastreadorHabitos.Core.ControlAcceso.DTOs;

// Anulables a propósito: la validación vive en el servicio, con mensajes en español [RD-07].
public class CambioContrasenaSolicitudDto
{
    public string? ContrasenaActual { get; set; }
    public string? ContrasenaNueva { get; set; }
}
