using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Services;

namespace RastreadorHabitos.Tests.ControlAcceso;

// [RF-CA-01] El correo de activación lleva a la página /activar del frontend, que llama a la API;
// el token del enlace sigue activando la cuenta una sola vez.
public class EnlaceActivacionTests
{
    private const string UrlBase = "http://localhost:5003";

    private sealed class CorreoCapturado : ISolicitudCorreoSaliente
    {
        public string UltimoCuerpo { get; private set; } = string.Empty;

        public Task SolicitarEnvioAsync(string destinatario, string asunto, string cuerpoHtml,
                                        CancellationToken cancellationToken = default)
        {
            UltimoCuerpo = cuerpoHtml;
            return Task.CompletedTask;
        }
    }

    private static ControlAccesoDbContext NuevaBase()
    {
        var contexto = new ControlAccesoDbContext(new DbContextOptionsBuilder<ControlAccesoDbContext>()
            .UseInMemoryDatabase($"EnlaceActivacion-{Guid.NewGuid():N}").Options);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    // [RF-CA-01] El enlace apunta a la página del frontend y no a la ruta de la API.
    [Fact]
    public async Task Registrar_EnviaEnlaceALaPaginaActivarDelFrontend()
    {
        // Preparar
        await using var contexto = NuevaBase();
        var correo = new CorreoCapturado();
        var servicio = new CuentaService(contexto, correo, new OpcionesEnlaces { UrlBase = UrlBase });

        // Actuar
        await servicio.RegistrarAsync(new RegistroSolicitudDto
        {
            Email = "ana@correo.com", Contrasena = "Habitos2026", NombreCompleto = "Ana Pérez"
        });

        // Verificar
        Assert.Matches(new Regex($"href=\"{Regex.Escape(UrlBase)}/activar\\?token=[0-9A-F]{{64}}\""), correo.UltimoCuerpo);
        Assert.DoesNotContain("/api/cuentas/activar", correo.UltimoCuerpo);
    }

    // [RF-CA-01] El token del enlace nuevo activa la cuenta con la operación de siempre.
    [Fact]
    public async Task Activar_ConElTokenDelEnlace_ConfirmaElCorreo()
    {
        // Preparar
        await using var contexto = NuevaBase();
        var correo = new CorreoCapturado();
        var servicio = new CuentaService(contexto, correo, new OpcionesEnlaces { UrlBase = UrlBase });
        await servicio.RegistrarAsync(new RegistroSolicitudDto
        {
            Email = "ana@correo.com", Contrasena = "Habitos2026", NombreCompleto = "Ana Pérez"
        });
        var token = Regex.Match(correo.UltimoCuerpo, "/activar\\?token=([0-9A-F]+)").Groups[1].Value;

        // Actuar
        await servicio.ActivarAsync(token);

        // Verificar
        var usuario = await contexto.Usuarios.SingleAsync();
        Assert.True(usuario.CorreoConfirmado);
    }
}
