using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.Notificaciones.DTOs;
using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Tests.Notificaciones;

// La bandeja del usuario: consultar solo las propias, marcar como leída y la preferencia de correo.
public class BandejaNotificacionesTests
{
    // [RF-NOT-04] CN-10: cada uno ve solo las suyas, nunca las de otro usuario.
    [Fact]
    public async Task ListarPropias_DevuelveSoloLasDelUsuarioYNingunaDeOtro()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var e1 = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var e2 = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        await piezas.Notificador().NotificarAsync(e1, "Prueba", "Para E1");
        await piezas.Notificador().NotificarAsync(e2, "Prueba", "Para E2");

        // Actuar
        var bandeja = await piezas.Bandeja().ListarPropiasAsync(e1, leida: null);

        // Verificar
        var unica = Assert.Single(bandeja.Notificaciones);
        Assert.Equal("Para E1", unica.Mensaje);
    }

    // [RF-NOT-04] CN-10: distingue leídas de no leídas, con el filtro y con el total de no leídas.
    [Fact]
    public async Task ListarPropias_DistingueLeidasDeNoLeidasYCuentaLasNoLeidas()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var usuario = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        await piezas.Notificador().NotificarAsync(usuario, "Prueba", "Primera");
        await piezas.Notificador().NotificarAsync(usuario, "Prueba", "Segunda");
        var primera = (await piezas.NotificacionesDeAsync(usuario)).Single(n => n.Mensaje == "Primera");
        await piezas.Bandeja().MarcarLeidaAsync(usuario, primera.Id);

        // Actuar
        var todas = await piezas.Bandeja().ListarPropiasAsync(usuario, leida: null);
        var noLeidas = await piezas.Bandeja().ListarPropiasAsync(usuario, leida: false);
        var leidas = await piezas.Bandeja().ListarPropiasAsync(usuario, leida: true);

        // Verificar
        Assert.Equal(2, todas.Notificaciones.Count);
        Assert.Equal(1, todas.NoLeidas);
        Assert.Equal("Segunda", Assert.Single(noLeidas.Notificaciones).Mensaje);
        Assert.Equal("Primera", Assert.Single(leidas.Notificaciones).Mensaje);
        Assert.Equal(1, leidas.NoLeidas); // el total de no leídas no depende del filtro
    }

    // [RF-NOT-05] CN-11: marcar una propia la deja leída con su fecha; marcarla otra vez no cambia nada.
    [Fact]
    public async Task MarcarLeida_NotificacionPropia_QuedaLeidaConSuFecha()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var usuario = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        await piezas.Notificador().NotificarAsync(usuario, "Prueba", "Mensaje");
        var id = Assert.Single(await piezas.NotificacionesDeAsync(usuario)).Id;

        // Actuar
        var marcada = await piezas.Bandeja().MarcarLeidaAsync(usuario, id);
        var otraVez = await piezas.Bandeja().MarcarLeidaAsync(usuario, id);

        // Verificar
        Assert.True(marcada.Leida);
        Assert.NotNull(marcada.FechaLecturaUtc);
        Assert.Equal(marcada.FechaLecturaUtc, otraVez.FechaLecturaUtc);
        Assert.Equal(0, (await piezas.Bandeja().ListarPropiasAsync(usuario, null)).NoLeidas);
    }

    // [RF-NOT-05] CN-12: marcar la notificación de otro usuario se rechaza y no la cambia.
    [Fact]
    public async Task MarcarLeida_NotificacionDeOtroUsuario_SeRechazaYSigueSinLeer()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var duena = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var otro = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        await piezas.Notificador().NotificarAsync(duena, "Prueba", "Mensaje");
        var id = Assert.Single(await piezas.NotificacionesDeAsync(duena)).Id;

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoNotificacionesException>(
            () => piezas.Bandeja().MarcarLeidaAsync(otro, id));

        // Verificar
        Assert.Equal(TipoRechazoNotificaciones.NoPermitido, rechazo.Tipo);
        Assert.False(Assert.Single(await piezas.NotificacionesDeAsync(duena)).Leida);
    }

    // [RF-NOT-05] CN-12: una notificación que no existe se rechaza como no encontrada.
    [Fact]
    public async Task MarcarLeida_NotificacionInexistente_SeRechazaComoNoEncontrada()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var usuario = await piezas.CrearUsuarioAsync(Rol.IdEstandar);

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoNotificacionesException>(
            () => piezas.Bandeja().MarcarLeidaAsync(usuario, Guid.NewGuid()));

        // Verificar
        Assert.Equal(TipoRechazoNotificaciones.NoEncontrado, rechazo.Tipo);
    }

    // [RF-NOT-07] CN-14: el correo está activo por defecto, se desactiva y se vuelve a activar.
    [Fact]
    public async Task Preferencias_SeGuardanPorUsuarioYEmpiezanConElCorreoActivo()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var usuario = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var inicial = await piezas.Bandeja().ObtenerPreferenciasAsync(usuario);

        // Actuar
        await piezas.Bandeja().ActualizarPreferenciasAsync(usuario, new ActualizacionPreferenciasDto { CorreoActivo = false });
        var desactivada = await piezas.Bandeja().ObtenerPreferenciasAsync(usuario);
        await piezas.Bandeja().ActualizarPreferenciasAsync(usuario, new ActualizacionPreferenciasDto { CorreoActivo = true });
        var reactivada = await piezas.Bandeja().ObtenerPreferenciasAsync(usuario);

        // Verificar
        Assert.True(inicial.CorreoActivo);
        Assert.False(desactivada.CorreoActivo);
        Assert.True(reactivada.CorreoActivo);
    }

    // [RF-NOT-07, RD-07] CN-15: una preferencia sin valor se rechaza y no cambia lo guardado.
    [Fact]
    public async Task ActualizarPreferencias_SinValor_SeRechazaYNoCambia()
    {
        // Preparar
        var piezas = new PiezasNotificaciones();
        var usuario = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        await piezas.DesactivarCorreoAsync(usuario);

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoNotificacionesException>(
            () => piezas.Bandeja().ActualizarPreferenciasAsync(usuario, new ActualizacionPreferenciasDto()));

        // Verificar
        Assert.Equal(TipoRechazoNotificaciones.DatosInvalidos, rechazo.Tipo);
        Assert.False((await piezas.Bandeja().ObtenerPreferenciasAsync(usuario)).CorreoActivo);
    }
}
