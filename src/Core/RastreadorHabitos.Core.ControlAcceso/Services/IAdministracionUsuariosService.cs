namespace RastreadorHabitos.Core.ControlAcceso.Services;

// Operaciones reservadas al Administrador. Quién puede invocarlas no se decide aquí: lo
// declara la tabla única de exigencias de rol de la Api [RF-CA-05].
public interface IAdministracionUsuariosService
{
    // Convierte en Administrador a un usuario ya registrado; así nace el primero [RF-CA-04].
    Task PromoverAdministradorAsync(string? email);
}
