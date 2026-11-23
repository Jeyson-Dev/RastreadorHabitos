using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.DTOs;
using RastreadorHabitos.Core.Notificaciones.Entities;

namespace RastreadorHabitos.Core.Notificaciones.Services;

public class NotificacionesService : INotificacionesService
{
    private readonly NotificacionesDbContext _context;

    public NotificacionesService(NotificacionesDbContext context)
    {
        _context = context;
    }

    public async Task<BandejaNotificacionesDto> ListarPropiasAsync(Guid usuarioId, bool? leida)
    {
        // El filtro por destinatario va siempre en la consulta: un usuario no ve las de otro
        // [RF-NOT-04].
        var propias = _context.Notificaciones.AsNoTracking().Where(n => n.DestinatarioId == usuarioId);

        var filtradas = leida is { } estado ? propias.Where(n => n.Leida == estado) : propias;
        var notificaciones = await filtradas
            .OrderByDescending(n => n.FechaUtc)
            .ToListAsync();

        return new BandejaNotificacionesDto
        {
            NoLeidas = await propias.CountAsync(n => !n.Leida),
            Notificaciones = notificaciones.Select(NotificacionDto.Desde).ToList()
        };
    }

    public async Task<NotificacionDto> MarcarLeidaAsync(Guid usuarioId, Guid notificacionId)
    {
        var notificacion = await _context.Notificaciones.FirstOrDefaultAsync(n => n.Id == notificacionId)
            ?? throw new RechazoNotificacionesException(TipoRechazoNotificaciones.NoEncontrado,
                "No existe una notificación con ese id.");

        // La entidad rechaza a quien no es su destinatario, sin cambiar nada [RF-NOT-05].
        notificacion.MarcarLeida(usuarioId, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        return NotificacionDto.Desde(notificacion);
    }

    public async Task<PreferenciasNotificacionDto> ObtenerPreferenciasAsync(Guid usuarioId)
    {
        var guardada = await _context.PreferenciasNotificacion.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

        return new PreferenciasNotificacionDto
        {
            CorreoActivo = guardada?.CorreoActivo ?? PreferenciaNotificacion.CorreoActivoPorDefecto
        };
    }

    public async Task<PreferenciasNotificacionDto> ActualizarPreferenciasAsync(Guid usuarioId,
                                                                              ActualizacionPreferenciasDto preferencias)
    {
        // Entrada inválida: rechazo controlado [RD-07].
        if (preferencias.CorreoActivo is not { } correoActivo)
        {
            throw new RechazoNotificacionesException(TipoRechazoNotificaciones.DatosInvalidos,
                "Indique si quiere recibir las notificaciones por correo (correoActivo: true o false).");
        }

        var guardada = await _context.PreferenciasNotificacion.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);
        if (guardada is null)
        {
            guardada = new PreferenciaNotificacion { UsuarioId = usuarioId };
            _context.PreferenciasNotificacion.Add(guardada);
        }

        guardada.CorreoActivo = correoActivo;
        guardada.FechaActualizacionUtc = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException) when (_context.Entry(guardada).State == EntityState.Added)
        {
            // Otra petición del mismo usuario creó su fila al mismo tiempo: se actualiza esa.
            _context.Entry(guardada).State = EntityState.Detached;
            var existente = await _context.PreferenciasNotificacion.FirstAsync(p => p.UsuarioId == usuarioId);
            existente.CorreoActivo = correoActivo;
            existente.FechaActualizacionUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return new PreferenciasNotificacionDto { CorreoActivo = correoActivo };
    }
}
