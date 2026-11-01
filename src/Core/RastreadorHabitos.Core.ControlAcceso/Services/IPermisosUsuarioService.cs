namespace RastreadorHabitos.Core.ControlAcceso.Services;

// Permisos concedidos a un usuario. Las demás piezas preguntan aquí, nunca a Gestión de
// permisos: tienePermiso(usuarioId, codigo) y aplicarPermiso(usuarioId, codigo).
public interface IPermisosUsuarioService
{
    // Si el usuario tiene concedido ese permiso.
    Task<bool> TienePermisoAsync(Guid usuarioId, string codigo);

    // Concede el permiso al usuario. Aplicarlo dos veces no lo duplica.
    Task AplicarPermisoAsync(Guid usuarioId, string codigo);
}
