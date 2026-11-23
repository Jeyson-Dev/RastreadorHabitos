using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Notificaciones.Configuracion;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.Entities;
using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Tests.Notificaciones;

// Regresión del Hallazgo 1 de la Asignación 3: un correo con destinatario vacío lanzaba una
// ArgumentException que nadie capturaba, el comando enviar-correos terminaba con error y los
// correos que venían detrás nunca salían (CC-04, RF-NOT-10).
public class Hallazgo1DestinatarioVacioTests
{
    // Transporte de prueba que, como MailMessage, rechaza un destinatario vacío con
    // ArgumentException y envía los demás.
    private sealed class TransporteQueRechazaVacios : ITransporteCorreo
    {
        public bool EstaConfigurado => true;
        public List<string> Enviados { get; } = [];

        public Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(destinatario, "to");
            Enviados.Add(destinatario);
            return Task.CompletedTask;
        }
    }

    // [Hallazgo 1] El transporte SMTP informa un destinatario vacío como un fallo del correo, igual
    // que uno mal formado, antes de intentar conectarse al servidor.
    [Fact]
    public async Task TransporteSmtp_ConDestinatarioVacio_LoInformaComoFalloDelCorreo()
    {
        // Preparar
        var transporte = new TransporteSmtp(new OpcionesSmtp { Host = "localhost", Puerto = 25, Remitente = "no-responder@prueba.local" });

        // Actuar
        var fallo = await Record.ExceptionAsync(() => transporte.EnviarAsync("", "Asunto", "<p>Cuerpo</p>", CancellationToken.None));

        // Verificar
        var esperado = Assert.IsType<FalloEnvioCorreoException>(fallo);
        Assert.Equal("La dirección del destinatario no es válida.", esperado.Message);
    }

    // [Hallazgo 1] Un error inesperado del transporte con un correo no detiene al procesador: ese
    // correo suma su intento y el que viene detrás sale.
    [Fact]
    public async Task Procesar_ConUnDestinatarioVacioPrimero_SumaSuIntentoYEnviaElSiguiente()
    {
        // Preparar
        var opciones = new DbContextOptionsBuilder<NotificacionesDbContext>().UseInMemoryDatabase($"H1-{Guid.NewGuid()}").Options;
        await using (var contexto = new NotificacionesDbContext(opciones))
        {
            contexto.CorreosEnCola.AddRange(
                new CorreoEnCola { Destinatario = "", Asunto = "A", CuerpoHtml = "x", FechaCreacionUtc = DateTime.UtcNow.AddSeconds(-1) },
                new CorreoEnCola { Destinatario = "ana@prueba.local", Asunto = "B", CuerpoHtml = "x", FechaCreacionUtc = DateTime.UtcNow });
            await contexto.SaveChangesAsync();
        }
        var transporte = new TransporteQueRechazaVacios();

        // Actuar
        var resultado = await new EnviadorColaCorreos(new NotificacionesDbContext(opciones), transporte).EnviarPendientesAsync();

        // Verificar
        await using var verificacion = new NotificacionesDbContext(opciones);
        var vacio = await verificacion.CorreosEnCola.SingleAsync(c => c.Destinatario == "");
        Assert.Equal(CorreoEnCola.EstadoPendiente, vacio.Estado);
        Assert.Equal(1, vacio.Intentos);
        Assert.NotNull(vacio.UltimoError);
        Assert.DoesNotContain("   at ", vacio.UltimoError);
        Assert.Equal(["ana@prueba.local"], transporte.Enviados);
        Assert.Equal(1, resultado.Enviados);
        Assert.Equal(1, resultado.ConError);
    }
}
