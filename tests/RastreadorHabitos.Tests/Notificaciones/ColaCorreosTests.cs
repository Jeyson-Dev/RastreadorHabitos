using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Api.Autorizacion;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.DTOs;
using RastreadorHabitos.Core.Notificaciones.Entities;
using RastreadorHabitos.Core.Notificaciones.Reglas;
using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Tests.Notificaciones;

// Transporte de correo de prueba: registra cada envío y falla siempre, o solo con los
// destinatarios indicados.
public sealed class TransporteFalso : ITransporteCorreo
{
    public bool EstaConfigurado { get; init; } = true;
    public bool FallarSiempre { get; set; }
    public HashSet<string> DestinatariosQueFallan { get; } = [];
    public List<string> Enviados { get; } = [];

    public Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken)
    {
        if (FallarSiempre || DestinatariosQueFallan.Contains(destinatario))
        {
            throw new FalloEnvioCorreoException("No se pudo enviar por el servidor de correo: Connection refused.");
        }
        Enviados.Add(destinatario);
        return Task.CompletedTask;
    }
}

// La cola completa: encolar sin enviar, el procesador, los reintentos y la consulta.
public class ColaCorreosTests
{
    private readonly DbContextOptions<NotificacionesDbContext> _opciones =
        new DbContextOptionsBuilder<NotificacionesDbContext>().UseInMemoryDatabase($"Cola-{Guid.NewGuid()}").Options;

    private NotificacionesDbContext Contexto() => new(_opciones);

    private Task EncolarAsync(string destinatario) =>
        new ColaCorreos(Contexto()).EncolarAsync(destinatario, "Asunto de prueba", "<p>Cuerpo</p>");

    private Task<ResultadoEnvioCorreos> ProcesarAsync(ITransporteCorreo transporte) =>
        new EnviadorColaCorreos(Contexto(), transporte).EnviarPendientesAsync();

    private async Task<CorreoEnCola> UnicoCorreoAsync()
    {
        await using var contexto = Contexto();
        return await contexto.CorreosEnCola.SingleAsync();
    }

    // [RF-NOT-08] CC-01: encolar no envía nada; el correo queda Pendiente con 0 intentos.
    [Fact]
    public async Task Encolar_NoEnviaYDejaElCorreoPendienteSinIntentos()
    {
        // Preparar
        var transporte = new TransporteFalso();

        // Actuar
        await EncolarAsync("ana@prueba.local");

        // Verificar
        var correo = await UnicoCorreoAsync();
        Assert.Equal(CorreoEnCola.EstadoPendiente, correo.Estado);
        Assert.Equal(0, correo.Intentos);
        Assert.Empty(transporte.Enviados);
    }

    // [RF-NOT-09] CC-02: el procesador envía los pendientes y los deja Enviado con 1 intento.
    [Fact]
    public async Task Procesar_ConElServidorDisponible_EnviaYDejaEnviadoConUnIntento()
    {
        // Preparar
        await EncolarAsync("ana@prueba.local");
        var transporte = new TransporteFalso();

        // Actuar
        var resultado = await ProcesarAsync(transporte);

        // Verificar
        var correo = await UnicoCorreoAsync();
        Assert.Equal(CorreoEnCola.EstadoEnviado, correo.Estado);
        Assert.Equal(1, correo.Intentos);
        Assert.NotNull(correo.FechaEnvioUtc);
        Assert.Equal(["ana@prueba.local"], transporte.Enviados);
        Assert.Equal(1, resultado.Enviados);
        Assert.True(resultado.Exito);
    }

    // [RF-NOT-12] CC-07: ejecutar el procesador dos veces seguidas no duplica ningún envío.
    [Fact]
    public async Task Procesar_DosVecesSeguidas_NoDuplicaElEnvio()
    {
        // Preparar
        await EncolarAsync("ana@prueba.local");
        var transporte = new TransporteFalso();
        await ProcesarAsync(transporte);
        var fechaEnvio = (await UnicoCorreoAsync()).FechaEnvioUtc;

        // Actuar
        var segunda = await ProcesarAsync(transporte);

        // Verificar
        Assert.Single(transporte.Enviados);
        Assert.Equal(0, segunda.Enviados);
        Assert.Equal(fechaEnvio, (await UnicoCorreoAsync()).FechaEnvioUtc);
    }

    // [RF-NOT-10] CC-03: un correo que falla sigue Pendiente con su intento y su último error.
    [Fact]
    public async Task Procesar_ConElServidorCaido_SumaUnIntentoYGuardaElUltimoError()
    {
        // Preparar
        await EncolarAsync("ana@prueba.local");
        var transporte = new TransporteFalso { FallarSiempre = true };

        // Actuar
        var resultado = await ProcesarAsync(transporte);

        // Verificar
        var correo = await UnicoCorreoAsync();
        Assert.Equal(CorreoEnCola.EstadoPendiente, correo.Estado);
        Assert.Equal(1, correo.Intentos);
        Assert.NotNull(correo.FechaUltimoIntentoUtc);
        Assert.Contains("Connection refused", correo.UltimoError);
        Assert.Equal(1, resultado.ConError);
        Assert.False(resultado.Exito);
    }

    // [RF-NOT-10] CC-03: al tercer intento fallido queda Fallido, y una cuarta ejecución no lo intenta.
    [Fact]
    public async Task Procesar_TresFallosSeguidos_LoDejaFallidoYNoLoVuelveAIntentar()
    {
        // Preparar
        await EncolarAsync("ana@prueba.local");
        var transporte = new TransporteFalso { FallarSiempre = true };
        for (var intento = 1; intento < ReglasCola.MaximoIntentos; intento++)
        {
            await ProcesarAsync(transporte);
        }

        // Actuar
        var tercera = await ProcesarAsync(transporte);
        transporte.FallarSiempre = false;
        var cuarta = await ProcesarAsync(transporte);

        // Verificar
        var correo = await UnicoCorreoAsync();
        Assert.Equal(CorreoEnCola.EstadoFallido, correo.Estado);
        Assert.Equal(ReglasCola.MaximoIntentos, correo.Intentos);
        Assert.NotNull(correo.UltimoError);
        Assert.Equal(1, tercera.PasaronAFallido);
        Assert.Equal(0, cuarta.Enviados);
        Assert.Empty(transporte.Enviados);
    }

    // [RF-NOT-10] CC-04: un correo con destinatario inválido no bloquea a los demás.
    [Fact]
    public async Task Procesar_UnDestinatarioQueFalla_NoImpideEnviarLosDemas()
    {
        // Preparar
        await EncolarAsync("sin-arroba");
        await EncolarAsync("ana@prueba.local");
        var transporte = new TransporteFalso();
        transporte.DestinatariosQueFallan.Add("sin-arroba");

        // Actuar
        await ProcesarAsync(transporte);

        // Verificar
        await using var contexto = Contexto();
        var correos = await contexto.CorreosEnCola.ToDictionaryAsync(c => c.Destinatario);
        Assert.Equal(CorreoEnCola.EstadoEnviado, correos["ana@prueba.local"].Estado);
        Assert.Equal(CorreoEnCola.EstadoPendiente, correos["sin-arroba"].Estado);
        Assert.Equal(1, correos["sin-arroba"].Intentos);
    }

    // [RF-NOT-10] CC-05: sin SMTP configurado no se consume ningún intento.
    [Fact]
    public async Task Procesar_SinSmtpConfigurado_NoConsumeIntentos()
    {
        // Preparar
        await EncolarAsync("ana@prueba.local");
        var transporte = new TransporteFalso { EstaConfigurado = false };

        // Actuar
        var resultado = await ProcesarAsync(transporte);

        // Verificar
        var correo = await UnicoCorreoAsync();
        Assert.False(resultado.SmtpConfigurado);
        Assert.Equal(0, correo.Intentos);
        Assert.Equal(CorreoEnCola.EstadoPendiente, correo.Estado);
    }

    // [RF-NOT-11] CC-06: la consulta muestra estado, intentos, fechas y último error, y filtra por estado.
    [Fact]
    public async Task ConsultarCola_FiltraPorEstadoYMuestraIntentosYUltimoError()
    {
        // Preparar
        await EncolarAsync("falla@prueba.local");
        await EncolarAsync("ana@prueba.local");
        var transporte = new TransporteFalso();
        transporte.DestinatariosQueFallan.Add("falla@prueba.local");
        for (var intento = 0; intento < ReglasCola.MaximoIntentos; intento++)
        {
            await ProcesarAsync(transporte);
        }

        // Actuar
        var todos = await new ConsultaColaCorreos(Contexto()).ListarAsync(null);
        var fallidos = await new ConsultaColaCorreos(Contexto()).ListarAsync("fallido");

        // Verificar
        Assert.Equal(2, todos.Count);
        var fallido = Assert.Single(fallidos);
        Assert.Equal("falla@prueba.local", fallido.Destinatario);
        Assert.Equal(CorreoEnCola.EstadoFallido, fallido.Estado);
        Assert.Equal(ReglasCola.MaximoIntentos, fallido.Intentos);
        Assert.NotNull(fallido.FechaUltimoIntentoUtc);
        Assert.NotNull(fallido.UltimoError);
    }

    // [RF-NOT-11, RD-07] CC-06: un estado que no existe se rechaza de forma controlada.
    [Fact]
    public async Task ConsultarCola_ConUnEstadoQueNoExiste_SeRechaza()
    {
        // Preparar
        var consulta = new ConsultaColaCorreos(Contexto());

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoNotificacionesException>(() => consulta.ListarAsync("Perdido"));

        // Verificar
        Assert.Equal(TipoRechazoNotificaciones.DatosInvalidos, rechazo.Tipo);
    }

    // [RF-NOT-11] CC-06: la consulta no expone el cuerpo, que lleva enlaces y códigos de otros usuarios.
    [Fact]
    public void ConsultarCola_NoDevuelveElCuerpoDeLosCorreos()
    {
        // Preparar
        var propiedades = typeof(CorreoEnColaDto).GetProperties().Select(p => p.Name);

        // Actuar
        var expuestas = propiedades.Where(p => p.Contains("Cuerpo", StringComparison.OrdinalIgnoreCase));

        // Verificar
        Assert.Empty(expuestas);
    }

    // [RF-NOT-11, RD-06] CC-06: consultar la cola está reservado al Administrador.
    [Fact]
    public void ConsultarCola_EstaReservadaAlAdministrador()
    {
        // Preparar
        const string controlador = "Correos", accion = "Listar";

        // Actuar
        var exigencia = ExigenciasDeRol.De(controlador, accion);

        // Verificar
        Assert.Equal(Exigencia.Administrador, exigencia);
    }
}
