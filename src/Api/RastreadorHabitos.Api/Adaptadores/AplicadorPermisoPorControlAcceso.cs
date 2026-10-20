using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Api.Adaptadores;

// Conecta el puerto de Gestión de permisos con Control de acceso, que es quien aplica el permiso
// aprobado. Sin lógica propia: el único permiso solicitable es un rol, y el rol vigente se lee
// de la base en cada petición, así el solicitante lo usa desde su siguiente petición [RF-GP-05].
public class AplicadorPermisoPorControlAcceso : IAplicadorPermiso
{
    private readonly IAdministracionUsuariosService _administracion;

    public AplicadorPermisoPorControlAcceso(IAdministracionUsuariosService administracion)
    {
        _administracion = administracion;
    }

    public Task AplicarAsync(Guid usuarioId, string permiso, Guid aprobadoPorId)
        => _administracion.CambiarRolAsync(usuarioId, aprobadoPorId, new CambioRolSolicitudDto { Rol = permiso });
}
