using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Entities;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public class PermisosUsuarioService : IPermisosUsuarioService
{
    private readonly ControlAccesoDbContext _context;

    public PermisosUsuarioService(ControlAccesoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TienePermisoAsync(Guid usuarioId, string codigo)
    {
        var codigoNormalizado = Normalizar(codigo);
        return await _context.PermisosUsuario
            .AnyAsync(p => p.UsuarioId == usuarioId && p.Codigo == codigoNormalizado);
    }

    public async Task AplicarPermisoAsync(Guid usuarioId, string codigo)
    {
        var codigoNormalizado = Normalizar(codigo);

        if (!await _context.Usuarios.AnyAsync(u => u.Id == usuarioId))
        {
            throw new RechazoControlAccesoException(MotivoRechazo.NoEncontrado,
                "No existe un usuario con ese id.");
        }

        if (await TienePermisoAsync(usuarioId, codigoNormalizado))
        {
            return;
        }

        _context.PermisosUsuario.Add(new PermisoUsuario { UsuarioId = usuarioId, Codigo = codigoNormalizado });
        await _context.SaveChangesAsync();
    }

    private static string Normalizar(string codigo) =>
        string.IsNullOrWhiteSpace(codigo)
            ? throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos, "El permiso es obligatorio.")
            : codigo.Trim().ToUpperInvariant();
}
