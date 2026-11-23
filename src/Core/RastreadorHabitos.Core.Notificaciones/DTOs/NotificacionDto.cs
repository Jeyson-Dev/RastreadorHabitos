using RastreadorHabitos.Core.Notificaciones.Entities;

namespace RastreadorHabitos.Core.Notificaciones.DTOs;

public class NotificacionDto
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public bool Leida { get; set; }
    public DateTime FechaUtc { get; set; }
    public DateTime? FechaLecturaUtc { get; set; }

    public static NotificacionDto Desde(Notificacion notificacion) => new()
    {
        Id = notificacion.Id,
        Tipo = notificacion.Tipo,
        Mensaje = notificacion.Mensaje,
        Leida = notificacion.Leida,
        FechaUtc = notificacion.FechaUtc,
        FechaLecturaUtc = notificacion.FechaLecturaUtc
    };
}

// Bandeja de un usuario: sus notificaciones y cuántas tiene sin leer [RF-NOT-04].
public class BandejaNotificacionesDto
{
    public int NoLeidas { get; set; }
    public List<NotificacionDto> Notificaciones { get; set; } = [];
}

public class PreferenciasNotificacionDto
{
    public bool CorreoActivo { get; set; }
}

// Entrada de PUT /api/notificaciones/preferencias. Anulable para distinguir «no lo envió» de
// false y rechazarlo con un mensaje controlado [RD-07].
public class ActualizacionPreferenciasDto
{
    public bool? CorreoActivo { get; set; }
}
