using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Auditoria.Data;
using RastreadorHabitos.Core.Auditoria.Entities;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.ControlAcceso.Reglas;

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

        // Cada usuario tiene exactamente un rol: promover reemplaza el rol Estándar [RF-CA-04]. Lo
        // ejecuta el comando de consola, sin sesión: actúa el sistema [RF-AUD-04].
        AplicarRol(usuario, Rol.IdAdministrador, RegistroAuditoria.Sistema);
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
        AplicarRol(usuario, rolId, administradorId);
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
        var ahoraUtc = DateTime.UtcNow;
        if (usuario.CuentaHabilitada)
        {
            usuario.CuentaHabilitada = false;
            _context.RegistrarEvento(administradorId, EventosAuditoria.UsuarioDesactivado, EventosAuditoria.Entidad,
                usuario.Id.ToString(), new { cuentaHabilitada = true }, new { cuentaHabilitada = false }, ahoraUtc);
        }

        // Sus sesiones abiertas se cierran en el mismo guardado: dejan de ser válidas ya, y una
        // reactivación posterior no revive credenciales emitidas antes. El registro de auditoría
        // entra en el mismo guardado [RF-AUD-04].
        await _context.CerrarSesionesAbiertasAsync(usuarioId, ahoraUtc);

        await _context.SaveChangesAsync();
    }

    public async Task ReactivarAsync(Guid usuarioId, Guid administradorId)
    {
        var usuario = await BuscarAsync(usuarioId);
        if (usuario.CuentaHabilitada)
        {
            return; // Ya estaba activa: no hay cambio que registrar.
        }

        usuario.CuentaHabilitada = true;
        _context.RegistrarEvento(administradorId, EventosAuditoria.UsuarioReactivado, EventosAuditoria.Entidad,
            usuario.Id.ToString(), new { cuentaHabilitada = false }, new { cuentaHabilitada = true }, DateTime.UtcNow);
        await _context.SaveChangesAsync();
    }

    // Cambia el rol y, si de verdad cambió, deja su registro en el mismo guardado [RF-AUD-04].
    private void AplicarRol(Usuario usuario, int rolId, Guid quienActua)
    {
        if (usuario.RolId == rolId)
        {
            return;
        }

        var anterior = NombreRol(usuario.RolId);
        usuario.RolId = rolId;
        _context.RegistrarEvento(quienActua, EventosAuditoria.RolCambiado, EventosAuditoria.Entidad,
            usuario.Id.ToString(), new { rol = anterior }, new { rol = NombreRol(rolId) }, DateTime.UtcNow);
    }

    private static string NombreRol(int rolId) =>
        rolId == Rol.IdAdministrador ? Rol.NombreAdministrador : Rol.NombreEstandar;

    private async Task<Usuario> BuscarAsync(Guid usuarioId) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId)
        ?? throw new RechazoControlAccesoException(MotivoRechazo.NoEncontrado, "El usuario no existe.");

    private const string EstadoActivo = "Activo";
    private const string EstadoPendienteActivacion = "Pendiente de activación";
    private const string EstadoDesactivado = "Desactivado";
}
