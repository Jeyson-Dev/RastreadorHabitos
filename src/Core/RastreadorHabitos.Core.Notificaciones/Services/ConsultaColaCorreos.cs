using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.DTOs;
using RastreadorHabitos.Core.Notificaciones.Entities;

namespace RastreadorHabitos.Core.Notificaciones.Services;

public class ConsultaColaCorreos : IConsultaColaCorreos
{
    private readonly NotificacionesDbContext _context;

    public ConsultaColaCorreos(NotificacionesDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CorreoEnColaDto>> ListarAsync(string? estado)
    {
        var consulta = _context.CorreosEnCola.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            var canonico = CorreoEnCola.Estados.FirstOrDefault(
                e => string.Equals(e, estado.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new RechazoNotificacionesException(TipoRechazoNotificaciones.DatosInvalidos,
                    $"El estado debe ser {string.Join(", ", CorreoEnCola.Estados)}.");
            consulta = consulta.Where(c => c.Estado == canonico);
        }

        // Proyección directa: el cuerpo ni siquiera se lee.
        return await consulta
            .OrderByDescending(c => c.FechaCreacionUtc)
            .Select(c => new CorreoEnColaDto
            {
                Id = c.Id,
                Destinatario = c.Destinatario,
                Asunto = c.Asunto,
                Estado = c.Estado,
                Intentos = c.Intentos,
                FechaCreacionUtc = c.FechaCreacionUtc,
                FechaUltimoIntentoUtc = c.FechaUltimoIntentoUtc,
                FechaEnvioUtc = c.FechaEnvioUtc,
                UltimoError = c.UltimoError
            })
            .ToListAsync();
    }
}
