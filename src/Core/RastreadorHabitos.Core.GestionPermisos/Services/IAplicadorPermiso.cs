namespace RastreadorHabitos.Core.GestionPermisos.Services;

// Puerto de salida: Gestión de permisos tramita la solicitud, pero quien aplica el permiso es
// Control de acceso. Api lo conecta, así esta pieza no autentica ni decide identidad [RF-GP-05].
public interface IAplicadorPermiso
{
    Task AplicarAsync(Guid usuarioId, string permiso, Guid aprobadoPorId);
}
