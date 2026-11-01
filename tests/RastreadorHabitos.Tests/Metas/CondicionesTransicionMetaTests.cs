using RastreadorHabitos.Modulo.Metas.Entities;
using RastreadorHabitos.Modulo.Metas.Reglas;
using RastreadorHabitos.Modulo.Metas.Services;
using static RastreadorHabitos.Tests.Metas.MetasDePrueba;

namespace RastreadorHabitos.Tests.Metas;

// Cada transición permitida exige su condición y la ejecuta solo quien le corresponde
// (columnas «Quién la ejecuta» y «Condición» de docs/maquina-de-estados.md). Si no se cumple, se
// rechaza y el estado no cambia [RD-04].
public class CondicionesTransicionMetaTests
{
    // Fila 1: sin ningún cumplimiento, Pendiente → En progreso se rechaza.
    [Fact]
    public void PendienteAEnProgreso_SinCumplimientos_SeRechazaYSiguePendiente()
    {
        // Preparar
        var meta = En(EstadoMeta.Pendiente);
        var contexto = new ContextoTransicion(EjecutorTransicion.Sistema, Avance: 0, Hoy);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.CambiarEstado(EstadoMeta.EnProgreso, contexto));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Pendiente, meta.Estado);
    }

    // Fila 1: el sistema no mueve una meta sin cumplimientos que todavía no venció.
    [Fact]
    public void AplicarTransicionesDelSistema_SinCumplimientosAntesDeVencer_LaMetaSiguePendiente()
    {
        // Preparar
        var meta = En(EstadoMeta.Pendiente);

        // Actuar
        meta.AplicarTransicionesDelSistema(avance: 0, Hoy);

        // Verificar
        Assert.Equal(EstadoMeta.Pendiente, meta.Estado);
    }

    // Fila 1: Pendiente → En progreso la ejecuta el sistema, no el dueño.
    [Fact]
    public void PendienteAEnProgreso_EjecutadaPorElDueno_SeRechazaYSiguePendiente()
    {
        // Preparar
        var meta = En(EstadoMeta.Pendiente);
        var contexto = new ContextoTransicion(EjecutorTransicion.Dueno, Avance: 1, Hoy);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.CambiarEstado(EstadoMeta.EnProgreso, contexto));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Pendiente, meta.Estado);
    }

    // Fila 2: con avance menor al objetivo, completar se rechaza.
    [Fact]
    public void EnProgresoACompletada_ConAvanceMenorAlObjetivo_SeRechazaYSigueEnProgreso()
    {
        // Preparar
        var meta = En(EstadoMeta.EnProgreso);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.Completar(avance: Objetivo - 1, Hoy));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.EnProgreso, meta.Estado);
    }

    // Fila 2: En progreso → Completada la ejecuta el dueño, no el sistema.
    [Fact]
    public void EnProgresoACompletada_EjecutadaPorElSistema_SeRechazaYSigueEnProgreso()
    {
        // Preparar
        var meta = En(EstadoMeta.EnProgreso);
        var contexto = new ContextoTransicion(EjecutorTransicion.Sistema, Avance: Objetivo, Hoy);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.CambiarEstado(EstadoMeta.Completada, contexto));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.EnProgreso, meta.Estado);
    }

    // Fila 3: antes de vencer la fecha límite, En progreso → Fallida se rechaza.
    [Fact]
    public void EnProgresoAFallida_AntesDeVencer_SeRechazaYSigueEnProgreso()
    {
        // Preparar
        var meta = En(EstadoMeta.EnProgreso);
        var contexto = new ContextoTransicion(EjecutorTransicion.Sistema, Avance: 1, FechaLimite);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.CambiarEstado(EstadoMeta.Fallida, contexto));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.EnProgreso, meta.Estado);
    }

    // Fila 3: vencida pero con el objetivo alcanzado, el sistema no la marca Fallida.
    [Fact]
    public void AplicarTransicionesDelSistema_VencidaConElObjetivoAlcanzado_LaMetaSigueEnProgreso()
    {
        // Preparar
        var meta = En(EstadoMeta.EnProgreso);

        // Actuar
        meta.AplicarTransicionesDelSistema(avance: Objetivo, Vencida);

        // Verificar
        Assert.Equal(EstadoMeta.EnProgreso, meta.Estado);
    }

    // Fila 4: antes de vencer la fecha límite, Pendiente → Fallida se rechaza.
    [Fact]
    public void PendienteAFallida_AntesDeVencer_SeRechazaYSiguePendiente()
    {
        // Preparar
        var meta = En(EstadoMeta.Pendiente);
        var contexto = new ContextoTransicion(EjecutorTransicion.Sistema, Avance: 0, FechaLimite);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.CambiarEstado(EstadoMeta.Fallida, contexto));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Pendiente, meta.Estado);
    }

    // Fila 5: una nueva fecha límite que no es posterior a hoy no reactiva la meta.
    [Fact]
    public void FallidaAEnProgreso_ConNuevaFechaLimiteDeHoy_SeRechazaYSigueFallidaConSuFecha()
    {
        // Preparar
        var meta = En(EstadoMeta.Fallida);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.Reactivar(Vencida, avance: 0, Vencida));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Fallida, meta.Estado);
        Assert.Equal(FechaLimite, meta.FechaLimite);
    }

    // Fila 5: Fallida → En progreso la ejecuta el dueño, no el sistema.
    [Fact]
    public void FallidaAEnProgreso_EjecutadaPorElSistema_SeRechazaYSigueFallida()
    {
        // Preparar
        var meta = En(EstadoMeta.Fallida);
        var contexto = new ContextoTransicion(EjecutorTransicion.Sistema, Avance: 0, Vencida, Vencida.AddDays(7));

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.CambiarEstado(EstadoMeta.EnProgreso, contexto));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Fallida, meta.Estado);
    }
}
