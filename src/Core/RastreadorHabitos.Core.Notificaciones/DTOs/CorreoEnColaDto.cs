namespace RastreadorHabitos.Core.Notificaciones.DTOs;

// Lo que el Administrador ve de cada correo de la cola [RF-NOT-11]. El cuerpo no se devuelve:
// lleva enlaces de activación y códigos de recuperación de otros usuarios.
public class CorreoEnColaDto
{
    public Guid Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int Intentos { get; set; }
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime? FechaUltimoIntentoUtc { get; set; }
    public DateTime? FechaEnvioUtc { get; set; }
    public string? UltimoError { get; set; }
}
