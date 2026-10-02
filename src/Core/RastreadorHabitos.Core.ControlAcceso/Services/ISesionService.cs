namespace RastreadorHabitos.Core.ControlAcceso.Services;

public interface ISesionService
{
    // Una credencial bien firmada solo sirve si su sesión sigue abierta [RF-CA-18].
    Task<bool> EstaAbiertaAsync(Guid sesionId);
}
