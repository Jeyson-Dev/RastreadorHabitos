namespace RastreadorHabitos.Modulo.Habitos.DTOs;

// Anulable a propósito: sin el dato, rechazo controlado [RD-07]. La fecha no se recibe: la
// asigna el servidor.
public class RegistroCumplimientoDto
{
    public bool? Cumplido { get; set; }
}
