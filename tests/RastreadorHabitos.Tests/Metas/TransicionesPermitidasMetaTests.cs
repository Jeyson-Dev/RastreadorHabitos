using RastreadorHabitos.Modulo.Metas.Entities;
using static RastreadorHabitos.Tests.Metas.MetasDePrueba;

namespace RastreadorHabitos.Tests.Metas;

// Tabla de transiciones permitidas de docs/maquina-de-estados.md: una prueba por fila, con su
// condición cumplida y ejecutada por quien le corresponde [RF-NEG-03, RD-04].
public class TransicionesPermitidasMetaTests
{
    // Estado inicial: toda meta nace Pendiente.
    [Fact]
    public void MetaNueva_NaceEnPendiente()
    {
        // Preparar: nada, la meta se crea en el paso siguiente.

        // Actuar
        var meta = new Meta { ObjetivoCumplimientos = Objetivo, FechaInicio = Hoy, FechaLimite = FechaLimite };

        // Verificar
        Assert.Equal(EstadoMeta.Pendiente, meta.Estado);
    }

    // Fila 1: Pendiente → En progreso, sistema, al menos un cumplimiento.
    [Fact]
    public void PendienteAEnProgreso_ConUnCumplimiento_ElSistemaLaPasaAEnProgreso()
    {
        // Preparar
        var meta = En(EstadoMeta.Pendiente);

        // Actuar
        meta.AplicarTransicionesDelSistema(avance: 1, Hoy);

        // Verificar
        Assert.Equal(EstadoMeta.EnProgreso, meta.Estado);
    }

    // Fila 2: En progreso → Completada, dueño, avance ≥ objetivo.
    [Fact]
    public void EnProgresoACompletada_ConAvanceIgualAlObjetivo_ElDuenoLaCompleta()
    {
        // Preparar
        var meta = En(EstadoMeta.EnProgreso);

        // Actuar
        meta.Completar(avance: Objetivo, Hoy);

        // Verificar
        Assert.Equal(EstadoMeta.Completada, meta.Estado);
    }

    // Fila 3: En progreso → Fallida, sistema, venció sin alcanzar el objetivo.
    [Fact]
    public void EnProgresoAFallida_AlVencerSinAlcanzarElObjetivo_ElSistemaLaMarcaFallida()
    {
        // Preparar
        var meta = En(EstadoMeta.EnProgreso);

        // Actuar
        meta.AplicarTransicionesDelSistema(avance: Objetivo - 1, Vencida);

        // Verificar
        Assert.Equal(EstadoMeta.Fallida, meta.Estado);
    }

    // Fila 4: Pendiente → Fallida, sistema, venció sin alcanzar el objetivo.
    [Fact]
    public void PendienteAFallida_AlVencerSinCumplimientos_ElSistemaLaMarcaFallida()
    {
        // Preparar
        var meta = En(EstadoMeta.Pendiente);

        // Actuar
        meta.AplicarTransicionesDelSistema(avance: 0, Vencida);

        // Verificar
        Assert.Equal(EstadoMeta.Fallida, meta.Estado);
    }

    // Fila 5: Fallida → En progreso, dueño, nueva fecha límite posterior a hoy.
    [Fact]
    public void FallidaAEnProgreso_ConNuevaFechaLimiteFutura_ElDuenoLaReactivaConEsaFecha()
    {
        // Preparar
        var meta = En(EstadoMeta.Fallida);
        var nuevaFechaLimite = Vencida.AddDays(7);

        // Actuar
        meta.Reactivar(nuevaFechaLimite, avance: 0, Vencida);

        // Verificar
        Assert.Equal(EstadoMeta.EnProgreso, meta.Estado);
        Assert.Equal(nuevaFechaLimite, meta.FechaLimite);
    }
}
