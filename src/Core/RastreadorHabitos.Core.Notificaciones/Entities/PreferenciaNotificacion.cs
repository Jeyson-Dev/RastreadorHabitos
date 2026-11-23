namespace RastreadorHabitos.Core.Notificaciones.Entities;

// Canales que el usuario quiere recibir. El interno no se puede apagar; el de correo sí
// [RF-NOT-07]. Sin fila guardada, el correo está activo.
public class PreferenciaNotificacion
{
    public const bool CorreoActivoPorDefecto = true;

    public Guid UsuarioId { get; set; }
    public bool CorreoActivo { get; set; } = CorreoActivoPorDefecto;
    public DateTime FechaActualizacionUtc { get; set; }
}
