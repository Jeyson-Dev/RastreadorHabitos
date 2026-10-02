using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.ControlAcceso.Reglas;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public class SesionService : ISesionService
{
    // Se compara contra este hash cuando el correo no existe, para que la respuesta tarde lo
    // mismo que con una contraseña incorrecta y el tiempo no revele qué correos existen.
    private static readonly Lazy<string> HashRelleno =
        new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), CuentaService.FactorTrabajoBCrypt));

    private readonly ControlAccesoDbContext _context;
    private readonly OpcionesSesion _opcionesSesion;
    private readonly IEmisorCredencialSesion _emisorCredencial;

    public SesionService(ControlAccesoDbContext context, OpcionesSesion opcionesSesion,
                         IEmisorCredencialSesion emisorCredencial)
    {
        _context = context;
        _opcionesSesion = opcionesSesion;
        _emisorCredencial = emisorCredencial;
    }

    public async Task<CredencialSesionDto> IniciarAsync(InicioSesionSolicitudDto solicitud)
    {
        var email = solicitud.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(solicitud.Contrasena))
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos,
                "El correo y la contraseña son obligatorios.");
        }

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null)
        {
            BCrypt.Net.BCrypt.Verify(solicitud.Contrasena, HashRelleno.Value);
            throw CredencialesIncorrectas();
        }

        // Durante el bloqueo se rechaza aun con la contraseña correcta, sin contar el intento [RF-CA-19].
        var ahoraUtc = DateTime.UtcNow;
        if (PoliticaBloqueo.EstaBloqueado(usuario, ahoraUtc))
        {
            throw new RechazoControlAccesoException(MotivoRechazo.Bloqueado,
                PoliticaBloqueo.MensajeBloqueo(usuario, ahoraUtc));
        }

        if (!BCrypt.Net.BCrypt.Verify(solicitud.Contrasena, usuario.PasswordHash))
        {
            PoliticaBloqueo.RegistrarFallo(usuario, ahoraUtc);
            await _context.SaveChangesAsync();
            throw CredencialesIncorrectas();
        }

        // Estos avisos solo llegan a quien conoce la contraseña.
        if (!usuario.CorreoConfirmado)
        {
            throw new RechazoControlAccesoException(MotivoRechazo.NoPermitido,
                "La cuenta no está activa. Revise su correo para activarla."); // [RF-CA-15]
        }
        if (!usuario.CuentaHabilitada)
        {
            throw new RechazoControlAccesoException(MotivoRechazo.NoPermitido, "La cuenta está desactivada.");
        }

        PoliticaBloqueo.RegistrarExito(usuario);
        var sesion = new SesionUsuario
        {
            UsuarioId = usuario.Id,
            FechaInicioUtc = ahoraUtc,
            FechaExpiracionUtc = ahoraUtc.Add(_opcionesSesion.Duracion)
        };
        _context.SesionesUsuario.Add(sesion);
        await _context.SaveChangesAsync();

        return new CredencialSesionDto
        {
            Token = _emisorCredencial.Emitir(sesion, usuario.Rol.Nombre),
            ExpiraUtc = sesion.FechaExpiracionUtc
        };
    }

    // Se resuelve a partir de la sesión, que ya se validó como abierta en esta petición [RF-CA-07].
    public async Task<UsuarioAutenticadoDto> ObtenerUsuarioAutenticadoAsync(Guid sesionId)
    {
        var usuario = await _context.SesionesUsuario
            .AsNoTracking()
            .Where(s => s.Id == sesionId)
            .Select(s => new UsuarioAutenticadoDto
            {
                Email = s.Usuario.Email,
                NombreCompleto = s.Usuario.NombreCompleto,
                Rol = s.Usuario.Rol.Nombre
            })
            .FirstOrDefaultAsync();

        return usuario ?? throw new RechazoControlAccesoException(MotivoRechazo.NoAutenticado,
            "Se requiere una sesión válida.");
    }

    public async Task CerrarAsync(Guid sesionId)
    {
        var sesion = await _context.SesionesUsuario.FirstOrDefaultAsync(s => s.Id == sesionId);
        if (sesion is null || sesion.FechaCierreUtc is not null)
        {
            return;
        }

        // Desde aquí EstaAbiertaAsync devuelve false y la credencial se rechaza en cada petición.
        sesion.FechaCierreUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<string?> ObtenerRolDeSesionAbiertaAsync(Guid sesionId)
    {
        var sesion = await _context.SesionesUsuario
            .AsNoTracking()
            .Include(s => s.Usuario)
            .ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(s => s.Id == sesionId);

        // Además de la sesión, se revisa al usuario: uno desactivado o sin correo confirmado
        // no puede seguir usando una credencial emitida antes.
        var abierta = sesion is not null
                      && sesion.EstaAbierta(DateTime.UtcNow)
                      && sesion.Usuario.CorreoConfirmado
                      && sesion.Usuario.CuentaHabilitada;

        return abierta ? sesion!.Usuario.Rol.Nombre : null;
    }

    // El mismo rechazo para correo inexistente y contraseña incorrecta [RF-CA-03].
    private static RechazoControlAccesoException CredencialesIncorrectas() =>
        new(MotivoRechazo.NoAutenticado, "Correo o contraseña incorrectos.");
}
