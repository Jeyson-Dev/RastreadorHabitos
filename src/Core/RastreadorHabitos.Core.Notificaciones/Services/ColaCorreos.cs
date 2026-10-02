using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.Entities;

namespace RastreadorHabitos.Core.Notificaciones.Services;

public class ColaCorreos : IColaCorreos
{
    private readonly NotificacionesDbContext _context;

    public ColaCorreos(NotificacionesDbContext context)
    {
        _context = context;
    }

    public async Task EncolarAsync(string destinatario, string asunto, string cuerpoHtml,
                                   CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinatario);
        ArgumentException.ThrowIfNullOrWhiteSpace(asunto);
        ArgumentNullException.ThrowIfNull(cuerpoHtml);

        // Solo se registra: el envío por SMTP es trabajo exclusivo del enviador de la cola,
        // así la operación que origina el correo termina bien aunque el servidor no responda.
        _context.CorreosEnCola.Add(new CorreoEnCola
        {
            Destinatario = destinatario,
            Asunto = asunto,
            CuerpoHtml = cuerpoHtml,
            Estado = CorreoEnCola.EstadoPendiente
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
