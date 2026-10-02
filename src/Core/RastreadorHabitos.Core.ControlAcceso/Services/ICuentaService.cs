using RastreadorHabitos.Core.ControlAcceso.DTOs;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public interface ICuentaService
{
    Task RegistrarAsync(RegistroSolicitudDto solicitud);
}
