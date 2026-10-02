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
