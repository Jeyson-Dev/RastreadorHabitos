using RastreadorHabitos.Core.ControlAcceso.DTOs;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

// Recuperación, restablecimiento y cambio de contraseña [RF-CA-09 a RF-CA-13, RF-CA-22].
public interface IContrasenaService
{
    // Termina igual exista o no el correo: el flujo no revela qué correos están registrados [RF-CA-09].
    Task SolicitarRecuperacionAsync(RecuperacionSolicitudDto solicitud);

    // Con un código válido define la contraseña nueva; las sesiones abiertas antes dejan de ser
    // válidas [RF-CA-10, RF-CA-11, RF-CA-12].
    Task RestablecerAsync(RestablecimientoSolicitudDto solicitud);

    // Exige la contraseña actual; aplican la política y el cierre de todas las sesiones,
    // incluida la que hace el cambio [RF-CA-22, RF-CA-14, RF-CA-12].
    Task CambiarAsync(Guid usuarioId, CambioContrasenaSolicitudDto solicitud);

    // Lo ordena un Administrador: la contraseña anterior deja de servir, se cierran las sesiones
    // y el usuario recibe por la cola el código para definir una nueva [RF-CA-13, RF-CA-12].
    Task ForzarRestablecimientoAsync(Guid usuarioId);
}
