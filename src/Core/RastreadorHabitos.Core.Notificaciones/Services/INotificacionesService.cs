using RastreadorHabitos.Core.Notificaciones.DTOs;

namespace RastreadorHabitos.Core.Notificaciones.Services;

// Lo que un usuario hace con sus propias notificaciones. Siempre sobre el usuario de la sesión,
// que la Api toma de Control de acceso: ninguna operación recibe el Id de otro usuario.
public interface INotificacionesService
{
    // Solo las del usuario, de la más reciente a la más antigua. Con leida = true o false, solo
    // las leídas o las no leídas; NoLeidas cuenta siempre todas las suyas sin leer [RF-NOT-04].
    Task<BandejaNotificacionesDto> ListarPropiasAsync(Guid usuarioId, bool? leida);

    // La de otro usuario se rechaza y no cambia [RF-NOT-05].
    Task<NotificacionDto> MarcarLeidaAsync(Guid usuarioId, Guid notificacionId);

    Task<PreferenciasNotificacionDto> ObtenerPreferenciasAsync(Guid usuarioId);

    // Apagar el correo no apaga el canal interno [RF-NOT-07].
    Task<PreferenciasNotificacionDto> ActualizarPreferenciasAsync(Guid usuarioId, ActualizacionPreferenciasDto preferencias);
}
