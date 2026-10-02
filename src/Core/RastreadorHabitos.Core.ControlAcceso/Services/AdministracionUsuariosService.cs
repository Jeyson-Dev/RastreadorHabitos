using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
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

    public async Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync()
    {
        // Proyección directa: el hash de la contraseña y los tokens ni siquiera se leen.
        return await _context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.FechaCreacionUtc)
            .Select(u => new UsuarioListadoDto
            {
                Id = u.Id,
                Email = u.Email,
                NombreCompleto = u.NombreCompleto,
                Rol = u.Rol.Nombre,
                Estado = !u.CuentaHabilitada ? EstadoDesactivado
                       : !u.CorreoConfirmado ? EstadoPendienteActivacion
                       : EstadoActivo
            })
            .ToListAsync();
    }

    private const string EstadoActivo = "Activo";
    private const string EstadoPendienteActivacion = "Pendiente de activación";
    private const string EstadoDesactivado = "Desactivado";
}
