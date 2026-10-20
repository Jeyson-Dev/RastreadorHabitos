using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Tests.GestionPermisos;

// Reemplaza a Control de acceso en las pruebas: registra qué permiso se le pidió aplicar y a quién.
public sealed class AplicadorPermisoFalso : IAplicadorPermiso
{
    public List<(Guid UsuarioId, string Permiso, Guid AprobadoPorId)> Aplicados { get; } = [];

    public Task AplicarAsync(Guid usuarioId, string permiso, Guid aprobadoPorId)
    {
        Aplicados.Add((usuarioId, permiso, aprobadoPorId));
        return Task.CompletedTask;
    }
}
