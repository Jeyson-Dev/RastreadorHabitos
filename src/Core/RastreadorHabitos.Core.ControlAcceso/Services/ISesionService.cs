using RastreadorHabitos.Core.ControlAcceso.DTOs;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public interface ISesionService
{
    Task<CredencialSesionDto> IniciarAsync(InicioSesionSolicitudDto solicitud);

    Task<UsuarioAutenticadoDto> ObtenerUsuarioAutenticadoAsync(Guid sesionId);

    // La credencial cerrada deja de servir: usarla después se rechaza [RF-CA-18].
    Task CerrarAsync(Guid sesionId);

    // Una credencial bien firmada solo sirve si su sesión sigue abierta [RF-CA-18]. Devuelve el
    // rol vigente del usuario, o null si la sesión no está abierta: un cambio de rol se aplica
    // en la siguiente petición, sin esperar a que venza la sesión [RF-CA-08].
    Task<string?> ObtenerRolDeSesionAbiertaAsync(Guid sesionId);
}
