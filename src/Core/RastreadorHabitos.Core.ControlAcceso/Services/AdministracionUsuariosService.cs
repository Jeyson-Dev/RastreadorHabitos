using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Entities;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public class AdministracionUsuariosService : IAdministracionUsuariosService
{
    private readonly ControlAccesoDbContext _context;

    public AdministracionUsuariosService(ControlAccesoDbContext context)
    {
        _context = context;
    }

    public async Task PromoverAdministradorAsync(string? email)
    {
        var emailNormalizado = email?.Trim().ToLowerInvariant();
        var usuario = string.IsNullOrEmpty(emailNormalizado)
            ? null
            : await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNormalizado);

        if (usuario is null)
        {
            throw new RechazoControlAccesoException(MotivoRechazo.NoEncontrado,
                "No existe un usuario registrado con ese correo.");
        }

        // Cada usuario tiene exactamente un rol: promover reemplaza el rol Estándar [RF-CA-04].
        usuario.RolId = Rol.IdAdministrador;
        await _context.SaveChangesAsync();
    }
}
