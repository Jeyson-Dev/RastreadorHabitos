using RastreadorHabitos.Core.ControlAcceso.DTOs;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public interface ISesionService
{
    Task<CredencialSesionDto> IniciarAsync(InicioSesionSolicitudDto solicitud);

    Task<UsuarioAutenticadoDto> ObtenerUsuarioAutenticadoAsync(Guid sesionId);

    // La credencial cerrada deja de servir: usarla después se rechaza [RF-CA-18].
    Task CerrarAsync(Guid sesionId);

    // Una credencial bien firmada solo sirve si su sesión sigue abierta [RF-CA-18].
    Task<bool> EstaAbiertaAsync(Guid sesionId);
}
