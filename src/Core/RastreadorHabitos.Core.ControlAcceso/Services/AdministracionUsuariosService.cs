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

    public async Task CambiarRolAsync(Guid usuarioId, Guid administradorId, CambioRolSolicitudDto solicitud)
    {
        var rolId = solicitud.Rol?.Trim().ToLowerInvariant() switch
        {
            "administrador" => Rol.IdAdministrador,
            "estandar" or "estándar" => Rol.IdEstandar,
            _ => throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos,
                $"El rol debe ser {Rol.NombreAdministrador} o {Rol.NombreEstandar}.")
        };

        // Evita que el sistema se quede sin Administradores por error.
        if (usuarioId == administradorId)
        {
            throw new RechazoControlAccesoException(MotivoRechazo.NoPermitido,
                "Un Administrador no puede cambiar su propio rol.");
        }

        var usuario = await BuscarAsync(usuarioId);

        // El cambio se aplica desde la siguiente petición del usuario: el rol se lee de la base.
        usuario.RolId = rolId;
        await _context.SaveChangesAsync();
    }

    public async Task DesactivarAsync(Guid usuarioId, Guid administradorId)
    {
        if (usuarioId == administradorId)
        {
            throw new RechazoControlAccesoException(MotivoRechazo.NoPermitido,
                "Un Administrador no puede desactivarse a sí mismo.");
        }

        var usuario = await BuscarAsync(usuarioId);
        usuario.CuentaHabilitada = false;

        // Sus sesiones abiertas se cierran en el mismo guardado: dejan de ser válidas ya, y una
        // reactivación posterior no revive credenciales emitidas antes.
        var ahoraUtc = DateTime.UtcNow;
        var sesionesAbiertas = await _context.SesionesUsuario
            .Where(s => s.UsuarioId == usuarioId && s.FechaCierreUtc == null)
            .ToListAsync();
        foreach (var sesion in sesionesAbiertas)
        {
            sesion.FechaCierreUtc = ahoraUtc;
        }

        await _context.SaveChangesAsync();
    }

    public async Task ReactivarAsync(Guid usuarioId)
    {
        var usuario = await BuscarAsync(usuarioId);
        usuario.CuentaHabilitada = true;
        await _context.SaveChangesAsync();
    }

    private async Task<Usuario> BuscarAsync(Guid usuarioId) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId)
        ?? throw new RechazoControlAccesoException(MotivoRechazo.NoEncontrado, "El usuario no existe.");

    private const string EstadoActivo = "Activo";
    private const string EstadoPendienteActivacion = "Pendiente de activación";
    private const string EstadoDesactivado = "Desactivado";
}
