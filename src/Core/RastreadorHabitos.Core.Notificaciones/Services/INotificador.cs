namespace RastreadorHabitos.Core.Notificaciones.Services;

// notificar(destinatarioId, tipo, mensaje): la operación que usan las demás piezas para avisarle
// algo a un usuario. Quién debe recibir el aviso lo decide quien notifica.
public interface INotificador
{
    // Deja la notificación en la bandeja interna del destinatario y, si tiene el canal de correo
    // activo, encola además el correo con el mismo mensaje [RF-NOT-06, RF-NOT-07]. Nunca envía el
    // correo: lo envía el procesador de la cola [RF-NOT-08].
    Task NotificarAsync(Guid destinatarioId, string tipo, string mensaje,
                        CancellationToken cancellationToken = default);
}
