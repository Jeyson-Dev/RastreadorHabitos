using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.ControlAcceso.Reglas;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public class CuentaService : ICuentaService
{
    private const int FactorTrabajoBCrypt = 12;
    private const int LongitudMaximaEmail = 256;
    private const int LongitudMaximaNombre = 150;
    private static readonly TimeSpan VigenciaEnlaceActivacion = TimeSpan.FromHours(24);

    private readonly ControlAccesoDbContext _context;
    private readonly ISolicitudCorreoSaliente _correoSaliente;
    private readonly OpcionesEnlaces _opcionesEnlaces;

    public CuentaService(ControlAccesoDbContext context, ISolicitudCorreoSaliente correoSaliente,
                         OpcionesEnlaces opcionesEnlaces)
    {
        _context = context;
        _correoSaliente = correoSaliente;
        _opcionesEnlaces = opcionesEnlaces;
    }

    public async Task RegistrarAsync(RegistroSolicitudDto solicitud)
    {
        var email = ValidarEmail(solicitud.Email);
        var nombreCompleto = ValidarNombreCompleto(solicitud.NombreCompleto);

        var incumplimiento = PoliticaContrasena.Validar(solicitud.Contrasena); // [RF-CA-14]
        if (incumplimiento is not null)
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos, incumplimiento);
        }

        if (await _context.Usuarios.AnyAsync(u => u.Email == email)) // [RF-CA-01]
        {
            throw CorreoYaRegistrado();
        }

        // El usuario nace con el correo sin confirmar [RF-CA-15].
        var usuario = new Usuario
        {
            Email = email,
            NombreCompleto = nombreCompleto,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(solicitud.Contrasena, FactorTrabajoBCrypt), // [RF-CA-02, RD-05]
            RolId = Rol.IdEstandar,
            CorreoConfirmado = false,
            CuentaHabilitada = true
        };
        _context.Usuarios.Add(usuario);
        var tokenPlano = AgregarTokenActivacion(usuario);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Dos registros simultáneos con el mismo correo: el índice único rechaza al segundo.
            _context.ChangeTracker.Clear();
            if (await _context.Usuarios.AnyAsync(u => u.Email == email))
            {
                throw CorreoYaRegistrado();
            }
            throw;
        }

        // Se pide después de guardar: si fallara, el usuario recupera el enlace con el reenvío [RF-CA-17].
        await SolicitarCorreoActivacionAsync(usuario, tokenPlano);
    }

    public async Task ActivarAsync(string? token)
    {
        TokenUsuario? tokenUsuario = null;
        if (!string.IsNullOrWhiteSpace(token))
        {
            var tokenHash = CalcularHashToken(token.Trim());
            tokenUsuario = await _context.TokensUsuario
                .Include(t => t.Usuario)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Tipo == TokenUsuario.TipoActivacionCuenta);
        }

        // Un solo mensaje para inexistente, usado, revocado o vencido: no revela cuál falló [RF-CA-16].
        var ahoraUtc = DateTime.UtcNow;
        if (tokenUsuario is null || !tokenUsuario.EsValido(ahoraUtc))
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos,
                "El enlace de activación no es válido, ya fue usado o venció.");
        }

        // Token consumido y correo confirmado en un solo guardado. CuentaHabilitada no se toca:
        // la controla el Administrador [RF-CA-20].
        tokenUsuario.FechaUsoUtc = ahoraUtc;
        tokenUsuario.Usuario.CorreoConfirmado = true;
        await _context.SaveChangesAsync();
    }

    private string AgregarTokenActivacion(Usuario usuario)
    {
        var tokenPlano = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        _context.TokensUsuario.Add(new TokenUsuario
        {
            UsuarioId = usuario.Id,
            TokenHash = CalcularHashToken(tokenPlano),
            Tipo = TokenUsuario.TipoActivacionCuenta,
            FechaExpiracionUtc = DateTime.UtcNow.Add(VigenciaEnlaceActivacion)
        });

        return tokenPlano;
    }

    private Task SolicitarCorreoActivacionAsync(Usuario usuario, string tokenPlano)
    {
        var enlace = $"{_opcionesEnlaces.UrlBase}/api/cuentas/activar?token={tokenPlano}";
        var nombre = WebUtility.HtmlEncode(usuario.NombreCompleto);

        var cuerpoHtml = $"""
            <p>Hola {nombre}:</p>
            <p>Para activar tu cuenta en Rastreador de hábitos, abre el siguiente enlace:</p>
            <p><a href="{enlace}">Activar mi cuenta</a></p>
            <p>El enlace vence en {VigenciaEnlaceActivacion.TotalHours} horas y solo puede usarse una vez.
            Si no creaste esta cuenta, ignora este correo.</p>
            """;

        return _correoSaliente.SolicitarEnvioAsync(usuario.Email, "Activa tu cuenta - Rastreador de hábitos", cuerpoHtml);
    }

    private static string ValidarEmail(string? email)
    {
        var valor = email?.Trim();

        if (string.IsNullOrEmpty(valor))
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos, "El correo es obligatorio.");
        }
        if (valor.Length > LongitudMaximaEmail)
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos,
                $"El correo no puede superar los {LongitudMaximaEmail} caracteres.");
        }
        if (!TieneFormatoDeEmail(valor))
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos, "El correo no tiene un formato válido.");
        }

        return valor.ToLowerInvariant();
    }

    private static bool TieneFormatoDeEmail(string email)
    {
        // MailAddress acepta formas como "Nombre <a@b.com>" o "a@b"; se exige la dirección
        // sola y un dominio con punto.
        if (!MailAddress.TryCreate(email, out var direccion)
            || direccion.Address != email
            || !string.IsNullOrEmpty(direccion.DisplayName))
        {
            return false;
        }

        var dominio = direccion.Host;
        return dominio.Contains('.') && !dominio.StartsWith('.') && !dominio.EndsWith('.');
    }

    private static string ValidarNombreCompleto(string? nombreCompleto)
    {
        var valor = nombreCompleto?.Trim();

        if (string.IsNullOrEmpty(valor))
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos, "El nombre completo es obligatorio.");
        }
        if (valor.Length > LongitudMaximaNombre)
        {
            throw new RechazoControlAccesoException(MotivoRechazo.DatosInvalidos,
                $"El nombre completo no puede superar los {LongitudMaximaNombre} caracteres.");
        }

        return valor;
    }

    private static RechazoControlAccesoException CorreoYaRegistrado() =>
        new(MotivoRechazo.Conflicto, "Ya existe una cuenta registrada con este correo.");

    // Solo se guarda el hash del token: quien lea la base no puede usar los enlaces.
    private static string CalcularHashToken(string tokenPlano) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenPlano)));
}
