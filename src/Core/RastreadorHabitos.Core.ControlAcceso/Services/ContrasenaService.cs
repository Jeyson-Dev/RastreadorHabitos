using System.Net;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.ControlAcceso.Reglas;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public class ContrasenaService : IContrasenaService
{
    private readonly ControlAccesoDbContext _context;
    private readonly ISolicitudCorreoSaliente _correoSaliente;

    public ContrasenaService(ControlAccesoDbContext context, ISolicitudCorreoSaliente correoSaliente)
    {
        _context = context;
        _correoSaliente = correoSaliente;
    }

    public async Task SolicitarRecuperacionAsync(RecuperacionSolicitudDto solicitud)
    {
        // Rechazar por formato no revela si el correo existe [RD-07].
        var email = CuentaService.ValidarEmail(solicitud.Email);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario is null)
        {
            return; // Misma respuesta que si existiera [RF-CA-09].
        }

        var codigo = await PrepararCodigoAsync(usuario.Id, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        // Sale por la cola, después de guardar el código [RF-CA-10, RF-NOT-08].
        await EnviarCodigoAsync(usuario, codigo,
            "Recibimos una solicitud para restablecer tu contraseña en Rastreador de hábitos.");
    }

    public async Task RestablecerAsync(RestablecimientoSolicitudDto solicitud)
    {
        // La política se valida antes de mirar el código, así un error no lo gasta [RF-CA-14].
        ValidarPolitica(solicitud.ContrasenaNueva);

        var email = solicitud.Email?.Trim().ToLowerInvariant();
        var usuario = string.IsNullOrEmpty(email)
            ? null
            : await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        var codigoHash = TokenUsuario.CalcularHash(CodigoRecuperacion.Normalizar(solicitud.Codigo));
        var codigo = usuario is null
            ? null
            : await _context.TokensUsuario.FirstOrDefaultAsync(t =>
                t.UsuarioId == usuario.Id
                && t.Tipo == TokenUsuario.TipoRecuperacionContrasena
                && t.TokenHash == codigoHash);

        // Un solo mensaje para inexistente, usado, revocado o vencido; la contraseña no cambia [RF-CA-10].
        var ahoraUtc = DateTime.UtcNow;
        if (usuario is null || codigo is null || !codigo.EsValido(ahoraUtc))
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos,
                "El código no es válido, ya fue usado o venció.");
        }

        // Todo en un solo guardado: contraseña nueva con hash, código consumido y sesiones cerradas.
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(solicitud.ContrasenaNueva, CuentaService.FactorTrabajoBCrypt); // [RF-CA-11]
        codigo.FechaUsoUtc = ahoraUtc;
        await _context.CerrarSesionesAbiertasAsync(usuario.Id, ahoraUtc); // [RF-CA-12]

        // Quien demuestra que controla el correo no sigue bloqueado por intentos con la contraseña vieja.
        PoliticaBloqueo.RegistrarExito(usuario);

        await _context.SaveChangesAsync();
    }

    private static void ValidarPolitica(string? contrasena)
    {
        var incumplimiento = PoliticaContrasena.Validar(contrasena);
        if (incumplimiento is not null)
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos, incumplimiento);
        }
    }

    // Revoca los códigos de recuperación vigentes del usuario y agrega uno nuevo, sin guardar:
    // así solo el último código enviado sirve. Devuelve el código en claro para el correo.
    private async Task<string> PrepararCodigoAsync(Guid usuarioId, DateTime ahoraUtc)
    {
        var vigentes = await _context.TokensUsuario
            .Where(t => t.UsuarioId == usuarioId
                        && t.Tipo == TokenUsuario.TipoRecuperacionContrasena
                        && t.FechaUsoUtc == null
                        && t.FechaRevocacionUtc == null)
            .ToListAsync();
        foreach (var anterior in vigentes)
        {
            anterior.FechaRevocacionUtc = ahoraUtc;
        }

        var codigo = CodigoRecuperacion.Generar();
        _context.TokensUsuario.Add(new TokenUsuario
        {
            UsuarioId = usuarioId,
            TokenHash = TokenUsuario.CalcularHash(CodigoRecuperacion.Normalizar(codigo)),
            Tipo = TokenUsuario.TipoRecuperacionContrasena,
            FechaCreacionUtc = ahoraUtc,
            FechaExpiracionUtc = ahoraUtc.Add(CodigoRecuperacion.Vigencia)
        });

        return codigo;
    }

    private Task EnviarCodigoAsync(Usuario usuario, string codigo, string motivo)
    {
        var nombre = WebUtility.HtmlEncode(usuario.NombreCompleto);
        var cuerpoHtml = $"""
            <p>Hola {nombre}:</p>
            <p>{motivo}</p>
            <p>Tu código para definir una contraseña nueva es: <strong>{codigo}</strong></p>
            <p>Vence en {CodigoRecuperacion.Vigencia.TotalMinutes} minutos y solo puede usarse una vez.
            Si no lo solicitaste, ignora este correo.</p>
            """;

        return _correoSaliente.SolicitarEnvioAsync(usuario.Email,
            "Código para restablecer tu contraseña - Rastreador de hábitos", cuerpoHtml);
    }
}
