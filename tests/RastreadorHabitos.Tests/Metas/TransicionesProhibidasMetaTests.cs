using RastreadorHabitos.Modulo.Metas.Entities;
using RastreadorHabitos.Modulo.Metas.Reglas;
using RastreadorHabitos.Modulo.Metas.Services;
using static RastreadorHabitos.Tests.Metas.MetasDePrueba;

namespace RastreadorHabitos.Tests.Metas;

// Tabla de transiciones prohibidas de docs/maquina-de-estados.md: una prueba por fila. La
// transición se intenta por cualquiera de los dos ejecutores, con un avance que alcanza el
// objetivo y una fecha límite nueva futura, de modo que solo la tabla puede rechazarla. Se
// verifica el rechazo y que el estado no cambió [RF-NEG-04].
public class TransicionesProhibidasMetaTests
{
    private static ContextoTransicion ContextoFavorable(EjecutorTransicion ejecutor) =>
        new(ejecutor, Avance: Objetivo, Hoy, NuevaFechaLimite: FechaLimite.AddDays(30));

    // Prohibida 1, con la operación de la API: completar una meta Pendiente, aun con el avance
    // suficiente, se rechaza.
    [Fact]
    public void Completar_MetaPendienteConAvanceSuficiente_SeRechazaYSiguePendiente()
    {
        // Preparar
        var meta = En(EstadoMeta.Pendiente);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.Completar(avance: Objetivo, Hoy));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Pendiente, meta.Estado);
    }

    // Prohibida 1: Pendiente → Completada.
    [Theory]
    [InlineData(EjecutorTransicion.Sistema)]
    [InlineData(EjecutorTransicion.Dueno)]
    public void PendienteACompletada_SeRechazaYSiguePendiente(EjecutorTransicion ejecutor)
    {
        // Preparar
        var meta = En(EstadoMeta.Pendiente);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() =>
            meta.CambiarEstado(EstadoMeta.Completada, ContextoFavorable(ejecutor)));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Pendiente, meta.Estado);
    }

    // Prohibida 2: En progreso → Pendiente.
    [Theory]
    [InlineData(EjecutorTransicion.Sistema)]
    [InlineData(EjecutorTransicion.Dueno)]
    public void EnProgresoAPendiente_SeRechazaYSigueEnProgreso(EjecutorTransicion ejecutor)
    {
        // Preparar
        var meta = En(EstadoMeta.EnProgreso);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() =>
            meta.CambiarEstado(EstadoMeta.Pendiente, ContextoFavorable(ejecutor)));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.EnProgreso, meta.Estado);
    }

    // Prohibida 3: Completada → Pendiente.
    [Theory]
    [InlineData(EjecutorTransicion.Sistema)]
    [InlineData(EjecutorTransicion.Dueno)]
    public void CompletadaAPendiente_SeRechazaYSigueCompletada(EjecutorTransicion ejecutor)
    {
        // Preparar
        var meta = En(EstadoMeta.Completada);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() =>
            meta.CambiarEstado(EstadoMeta.Pendiente, ContextoFavorable(ejecutor)));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Completada, meta.Estado);
    }

    // Prohibida 4: Completada → En progreso.
    [Theory]
    [InlineData(EjecutorTransicion.Sistema)]
    [InlineData(EjecutorTransicion.Dueno)]
    public void CompletadaAEnProgreso_SeRechazaYSigueCompletada(EjecutorTransicion ejecutor)
    {
        // Preparar
        var meta = En(EstadoMeta.Completada);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() =>
            meta.CambiarEstado(EstadoMeta.EnProgreso, ContextoFavorable(ejecutor)));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Completada, meta.Estado);
    }

    // Prohibida 5: Completada → Fallida.
    [Theory]
    [InlineData(EjecutorTransicion.Sistema)]
    [InlineData(EjecutorTransicion.Dueno)]
    public void CompletadaAFallida_SeRechazaYSigueCompletada(EjecutorTransicion ejecutor)
    {
        // Preparar
        var meta = En(EstadoMeta.Completada);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() =>
            meta.CambiarEstado(EstadoMeta.Fallida, ContextoFavorable(ejecutor)));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Completada, meta.Estado);
    }

    // Prohibida 6: Fallida → Completada.
    [Theory]
    [InlineData(EjecutorTransicion.Sistema)]
    [InlineData(EjecutorTransicion.Dueno)]
    public void FallidaACompletada_SeRechazaYSigueFallida(EjecutorTransicion ejecutor)
    {
        // Preparar
        var meta = En(EstadoMeta.Fallida);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() =>
            meta.CambiarEstado(EstadoMeta.Completada, ContextoFavorable(ejecutor)));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Fallida, meta.Estado);
    }

    // Prohibida 7: Fallida → Pendiente.
    [Theory]
    [InlineData(EjecutorTransicion.Sistema)]
    [InlineData(EjecutorTransicion.Dueno)]
    public void FallidaAPendiente_SeRechazaYSigueFallida(EjecutorTransicion ejecutor)
    {
        // Preparar
        var meta = En(EstadoMeta.Fallida);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() =>
            meta.CambiarEstado(EstadoMeta.Pendiente, ContextoFavorable(ejecutor)));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Fallida, meta.Estado);
    }
}
