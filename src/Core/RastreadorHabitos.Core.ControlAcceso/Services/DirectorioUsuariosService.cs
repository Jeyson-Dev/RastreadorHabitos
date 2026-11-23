using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Entities;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public class DirectorioUsuariosService : IDirectorioUsuarios
{
    private readonly ControlAccesoDbContext _context;

    public DirectorioUsuariosService(ControlAccesoDbContext context)
    {
        _context = context;
    }

    // «Activo» es lo mismo que exige el inicio de sesión: correo confirmado y cuenta habilitada.
    public async Task<IReadOnlyList<Guid>> ListarAdministradoresActivosAsync(CancellationToken cancellationToken = default) =>
        await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.RolId == Rol.IdAdministrador && u.CorreoConfirmado && u.CuentaHabilitada)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

    public async Task<string?> ObtenerCorreoAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
}
