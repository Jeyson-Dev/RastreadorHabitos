using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.Notificaciones.Entities;
using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Tests.Notificaciones;

// notificar: canal interno siempre y canal de correo según la preferencia del destinatario.
public class NotificadorTests
{
    // [RF-NOT-06] CN-13: la notificación queda en la bandeja y su correo en la cola, Pendiente.
    [Fact]
    public async Task Notificar_ConElCorreoActivo_DejaLaInternaYEncolaElCorreoPendiente()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var usuario = await piezas.CrearUsuarioAsync(Rol.IdEstandar);

        // Actuar
        await piezas.Notificador().NotificarAsync(usuario, "Prueba", "Mensaje de prueba");

        // Verificar
        var interna = Assert.Single(await piezas.NotificacionesDeAsync(usuario));
        Assert.Equal("Prueba", interna.Tipo);
        Assert.Equal("Mensaje de prueba", interna.Mensaje);
        Assert.False(interna.Leida);
        var correo = Assert.Single(await piezas.CorreosAsync());
        Assert.Equal(await piezas.CorreoDeAsync(usuario), correo.Destinatario);
        Assert.Equal(CorreoEnCola.EstadoPendiente, correo.Estado);
        Assert.Contains("Mensaje de prueba", correo.CuerpoHtml);
        Assert.Null(correo.FechaEnvioUtc);
    }

    // [RF-NOT-07] CN-14: con el correo desactivado la interna aparece y no se encola ningún correo.
    [Fact]
    public async Task Notificar_ConElCorreoDesactivado_DejaLaInternaYNoEncolaCorreo()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var usuario = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        await piezas.DesactivarCorreoAsync(usuario);

        // Actuar
        await piezas.Notificador().NotificarAsync(usuario, "Prueba", "Mensaje de prueba");

        // Verificar
        Assert.Single(await piezas.NotificacionesDeAsync(usuario));
        Assert.Empty(await piezas.CorreosAsync());
    }

    // [RF-NOT-07] CN-14: la preferencia es de cada usuario; la de uno no cambia lo que recibe otro.
    [Fact]
    public async Task Notificar_ElCorreoDesactivadoDeUnUsuario_NoAfectaAOtro()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var conCorreo = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var sinCorreo = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        await piezas.DesactivarCorreoAsync(sinCorreo);

        // Actuar
        await piezas.Notificador().NotificarAsync(conCorreo, "Prueba", "Para el que tiene correo");
        await piezas.Notificador().NotificarAsync(sinCorreo, "Prueba", "Para el que no tiene correo");

        // Verificar
        var correo = Assert.Single(await piezas.CorreosAsync());
        Assert.Equal(await piezas.CorreoDeAsync(conCorreo), correo.Destinatario);
    }

    // [RF-NOT-06] CT-01: la notificación y su correo se crean con la misma hora, en UTC.
    [Fact]
    public async Task Notificar_LaInternaYSuCorreo_TienenLaMismaHoraEnUtc()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var usuario = await piezas.CrearUsuarioAsync(Rol.IdEstandar);

        // Actuar
        await piezas.Notificador().NotificarAsync(usuario, "Prueba", "Mensaje");

        // Verificar
        var interna = Assert.Single(await piezas.NotificacionesDeAsync(usuario));
        var correo = Assert.Single(await piezas.CorreosAsync());
        Assert.Equal(interna.FechaUtc, correo.FechaCreacionUtc);
        Assert.Equal(DateTimeKind.Utc, interna.FechaUtc.Kind);
    }

    // [RF-NOT-06] El texto del mensaje (por ejemplo el motivo de un rechazo) no se interpreta como
    // HTML en el correo.
    [Fact]
    public async Task Notificar_MensajeConHtml_SeCodificaEnElCuerpoDelCorreo()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var usuario = await piezas.CrearUsuarioAsync(Rol.IdEstandar);

        // Actuar
        await piezas.Notificador().NotificarAsync(usuario, "Prueba", "<script>alert(1)</script>");

        // Verificar
        var correo = Assert.Single(await piezas.CorreosAsync());
        Assert.DoesNotContain("<script>", correo.CuerpoHtml);
        Assert.Contains("&lt;script&gt;", correo.CuerpoHtml);
        Assert.Equal(Notificador.AsuntoCorreo, correo.Asunto);
    }
}
