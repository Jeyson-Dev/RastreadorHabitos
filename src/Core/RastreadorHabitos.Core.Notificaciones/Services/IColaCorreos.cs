namespace RastreadorHabitos.Core.Notificaciones.Services;

public interface IColaCorreos
{
    // Registra el correo como pendiente; nunca lo envía [RF-NOT-08].
    Task EncolarAsync(string destinatario, string asunto, string cuerpoHtml,
                      CancellationToken cancellationToken = default);
}
