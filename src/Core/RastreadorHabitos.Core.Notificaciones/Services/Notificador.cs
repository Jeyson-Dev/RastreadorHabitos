using System.Net;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.Entities;

namespace RastreadorHabitos.Core.Notificaciones.Services;

public class Notificador : INotificador
{
    public const int LongitudMaximaTipo = 50;
    public const string AsuntoCorreo = "Tienes una notificación nueva - Rastreador de hábitos";

    private readonly NotificacionesDbContext _context;
    private readonly IDirectorioDestinatarios _directorio;

    public Notificador(NotificacionesDbContext context, IDirectorioDestinatarios directorio)
    {
        _context = context;
        _directorio = directorio;
    }

    public async Task NotificarAsync(Guid destinatarioId, string tipo, string mensaje,
                                     CancellationToken cancellationToken = default)
    {
        // Quien notifica es otra pieza del sistema, no el usuario: un dato vacío es un error de
        // programación, no una entrada a validar con un mensaje.
        ArgumentException.ThrowIfNullOrWhiteSpace(tipo);
        ArgumentException.ThrowIfNullOrWhiteSpace(mensaje);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tipo.Length, LongitudMaximaTipo);

        // La notificación y su correo comparten la misma hora [RD-11].
        var ahora = DateTime.UtcNow;

        // Canal interno: siempre [RF-NOT-06]; no se puede desactivar [RF-NOT-07].
        _context.Notificaciones.Add(new Notificacion(destinatarioId, tipo, mensaje, ahora));

        // Canal de correo: solo si el destinatario no lo desactivó. Con el correo desactivado no
        // se encola nada [RF-NOT-07].
        if (await CorreoActivoAsync(destinatarioId, cancellationToken)
            && await _directorio.ObtenerCorreoAsync(destinatarioId, cancellationToken) is { } correo)
        {
            _context.CorreosEnCola.Add(new CorreoEnCola
            {
                Destinatario = correo,
                Asunto = AsuntoCorreo,
                CuerpoHtml = CuerpoHtml(mensaje),
                Estado = CorreoEnCola.EstadoPendiente,
                FechaCreacionUtc = ahora
            });
        }

        // Un solo guardado: la notificación interna y su correo quedan las dos o ninguna.
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> CorreoActivoAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        await _context.PreferenciasNotificacion
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => (bool?)p.CorreoActivo)
            .FirstOrDefaultAsync(cancellationToken)
        ?? PreferenciaNotificacion.CorreoActivoPorDefecto;

    // El mensaje puede llevar texto que escribió un usuario (por ejemplo el motivo de un
    // rechazo): se codifica para que no se interprete como HTML.
    private static string CuerpoHtml(string mensaje) => $"""
        <p>Tienes una notificación nueva en el Rastreador de hábitos:</p>
        <p>{WebUtility.HtmlEncode(mensaje)}</p>
        <p>También la encuentras en tu bandeja de notificaciones dentro del sistema.</p>
        """;
}
