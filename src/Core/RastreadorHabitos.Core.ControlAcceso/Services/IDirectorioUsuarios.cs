namespace RastreadorHabitos.Core.ControlAcceso.Services;

// Consultas de solo lectura sobre la identidad de los usuarios, para las piezas que necesitan
// saber a quién avisar sin leer las tablas de Control de acceso. No cambian nada.
public interface IDirectorioUsuarios
{
    // Ids de los Administradores con la cuenta activada y no desactivada [RF-NOT-01].
    Task<IReadOnlyList<Guid>> ListarAdministradoresActivosAsync(CancellationToken cancellationToken = default);

    // Correo del usuario, o null si no existe [RF-NOT-06].
    Task<string?> ObtenerCorreoAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
