using RastreadorHabitos.Core.ControlAcceso.DTOs;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

// Operaciones reservadas al Administrador. Quién puede invocarlas no se decide aquí: lo
// declara la tabla única de exigencias de rol de la Api [RF-CA-05].
public interface IAdministracionUsuariosService
{
    // Convierte en Administrador a un usuario ya registrado; así nace el primero [RF-CA-04].
    Task PromoverAdministradorAsync(string? email);

    // Usuarios con su rol y su estado, sin datos sensibles [RF-CA-21].
    Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync();

    // Un Administrador no puede cambiar su propio rol [RF-CA-08].
    Task CambiarRolAsync(Guid usuarioId, Guid administradorId, CambioRolSolicitudDto solicitud);
}
