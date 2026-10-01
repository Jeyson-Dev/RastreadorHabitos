namespace RastreadorHabitos.Core.Notificaciones.Entities;

public class CorreoEnCola
{
    // Versión mínima de la cola [RF-NOT-08, RF-NOT-09]. El estado fallido, los intentos
    // y el último error llegan en la semana 11, con la pieza 4.
    public const string EstadoPendiente = "Pendiente";
    public const string EstadoEnviado = "Enviado";

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string CuerpoHtml { get; set; } = string.Empty;
    public string Estado { get; set; } = EstadoPendiente;
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEnvioUtc { get; set; }
}
