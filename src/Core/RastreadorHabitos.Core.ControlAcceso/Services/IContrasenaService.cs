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
}
