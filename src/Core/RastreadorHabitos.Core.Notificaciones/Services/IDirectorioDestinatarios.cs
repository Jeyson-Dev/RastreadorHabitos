namespace RastreadorHabitos.Core.Notificaciones.Services;

// Puerto de salida: para entregar una notificación por correo hace falta la dirección del
// destinatario, que es de Control de acceso. Esta pieza no lo conoce: la Api conecta el puerto.
public interface IDirectorioDestinatarios
{
    // Correo del usuario, o null si no existe.
    Task<string?> ObtenerCorreoAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
