using RastreadorHabitos.Modulo.Metas.Entities;
using RastreadorHabitos.Modulo.Metas.Reglas;
using RastreadorHabitos.Modulo.Metas.Services;
using static RastreadorHabitos.Tests.Metas.MetasDePrueba;

namespace RastreadorHabitos.Tests.Metas;

// Completada es el estado terminal: ninguna transición parte de él y de él no se sale
// [RF-NEG-05].
public class EstadoTerminalMetaTests
{
    // En la tabla de transiciones permitidas ninguna sale de Completada.
    [Fact]
    public void Completada_EsTerminal_NingunaTransicionPermitidaSaleDeElla()
    {
        // Preparar
        var destinos = Enum.GetValues<EstadoMeta>();

        // Actuar
        var esTerminal = TransicionesMeta.EsTerminal(EstadoMeta.Completada);
        var salidas = destinos.Where(hacia => TransicionesMeta.EsPermitida(EstadoMeta.Completada, hacia)).ToList();

        // Verificar
        Assert.True(esTerminal);
        Assert.Empty(salidas);
    }

    // Una meta Completada no sale hacia ningún estado, la intente quien la intente.
    [Theory]
    [InlineData(EstadoMeta.Pendiente, EjecutorTransicion.Sistema)]
    [InlineData(EstadoMeta.Pendiente, EjecutorTransicion.Dueno)]
    [InlineData(EstadoMeta.EnProgreso, EjecutorTransicion.Sistema)]
    [InlineData(EstadoMeta.EnProgreso, EjecutorTransicion.Dueno)]
    [InlineData(EstadoMeta.Completada, EjecutorTransicion.Sistema)]
    [InlineData(EstadoMeta.Completada, EjecutorTransicion.Dueno)]
    [InlineData(EstadoMeta.Fallida, EjecutorTransicion.Sistema)]
    [InlineData(EstadoMeta.Fallida, EjecutorTransicion.Dueno)]
    public void MetaCompletada_NoSaleHaciaNingunEstado(EstadoMeta hacia, EjecutorTransicion ejecutor)
    {
        // Preparar
        var meta = En(EstadoMeta.Completada);
        var contexto = new ContextoTransicion(ejecutor, Avance: 0, Vencida, NuevaFechaLimite: Vencida.AddDays(30));

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.CambiarEstado(hacia, contexto));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Completada, meta.Estado);
    }

    // Reactivar una meta Completada, con la operación de la API, se rechaza.
    [Fact]
    public void Reactivar_MetaCompletada_SeRechazaYSigueCompletadaConSuFecha()
    {
        // Preparar
        var meta = En(EstadoMeta.Completada);

        // Actuar
        var rechazo = Assert.Throws<RechazoMetasException>(() => meta.Reactivar(FechaLimite.AddDays(30), avance: Objetivo, Hoy));

        // Verificar
        Assert.Equal(TipoRechazoMetas.TransicionNoPermitida, rechazo.Tipo);
        Assert.Equal(EstadoMeta.Completada, meta.Estado);
        Assert.Equal(FechaLimite, meta.FechaLimite);
    }

    // Vencida y sin avance, el sistema tampoco mueve una meta Completada.
    [Fact]
    public void AplicarTransicionesDelSistema_MetaCompletadaVencida_SigueCompletada()
    {
        // Preparar
        var meta = En(EstadoMeta.Completada);

        // Actuar
        meta.AplicarTransicionesDelSistema(avance: 0, Vencida);

        // Verificar
        Assert.Equal(EstadoMeta.Completada, meta.Estado);
    }

    // Los demás estados no son terminales: de cada uno sale al menos una transición.
    [Theory]
    [InlineData(EstadoMeta.Pendiente)]
    [InlineData(EstadoMeta.EnProgreso)]
    [InlineData(EstadoMeta.Fallida)]
    public void EstadoNoTerminal_TieneAlMenosUnaSalida(EstadoMeta estado)
    {
        // Preparar: el estado llega por parámetro.

        // Actuar
        var esTerminal = TransicionesMeta.EsTerminal(estado);

        // Verificar
        Assert.False(esTerminal);
    }
}
