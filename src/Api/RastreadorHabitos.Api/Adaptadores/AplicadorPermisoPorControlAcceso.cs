using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.GestionPermisos.Reglas;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Api.Adaptadores;

// Conecta el puerto de Gestión de permisos con Control de acceso, que es quien aplica el permiso
// aprobado. Sin lógica propia: el permiso Administrador es un rol y se aplica cambiando el rol;
// cualquier otro se aplica con aplicarPermiso. Control de acceso lee ambos de la base en cada
// petición, así el solicitante lo usa desde su siguiente petición [RF-GP-05].
public class AplicadorPermisoPorControlAcceso : IAplicadorPermiso
{
    private readonly IAdministracionUsuariosService _administracion;
    private readonly IPermisosUsuarioService _permisos;

    public AplicadorPermisoPorControlAcceso(IAdministracionUsuariosService administracion,
                                            IPermisosUsuarioService permisos)
    {
        _administracion = administracion;
        _permisos = permisos;
    }

    public Task AplicarAsync(Guid usuarioId, string permiso, Guid aprobadoPorId)
        => permiso == PermisosSolicitables.Administrador
            ? _administracion.CambiarRolAsync(usuarioId, aprobadoPorId, new CambioRolSolicitudDto { Rol = permiso })
            : _permisos.AplicarPermisoAsync(usuarioId, permiso);
}
