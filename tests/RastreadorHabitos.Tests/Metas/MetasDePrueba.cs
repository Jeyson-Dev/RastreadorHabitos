using RastreadorHabitos.Modulo.Metas.Entities;

namespace RastreadorHabitos.Tests.Metas;

// Metas en un estado dado, construidas solo con transiciones permitidas, sin base de datos ni
// aplicación [RD-12]. Cada llamada entrega una meta nueva: ninguna prueba comparte estado.
public static class MetasDePrueba
{
    public const int Objetivo = 3;

    // Un día cualquiera, fijo para que las pruebas no dependan de la fecha en que se ejecutan.
    public static readonly DateOnly Hoy = new(2026, 11, 10);
    public static readonly DateOnly FechaLimite = Hoy.AddDays(10);

    // Primer día después de la fecha límite: la meta ya venció.
    public static readonly DateOnly Vencida = FechaLimite.AddDays(1);

    public static Meta En(EstadoMeta estado)
    {
        var meta = new Meta
        {
            UsuarioId = Guid.NewGuid(),
            Nombre = "Leer 3 veces",
            ObjetivoCumplimientos = Objetivo,
            FechaInicio = Hoy,
            FechaLimite = FechaLimite
        };

        switch (estado)
        {
            case EstadoMeta.EnProgreso:
                meta.AplicarTransicionesDelSistema(avance: 1, Hoy);
                break;
            case EstadoMeta.Completada:
                meta.AplicarTransicionesDelSistema(avance: 1, Hoy);
                meta.Completar(avance: Objetivo, Hoy);
                break;
            case EstadoMeta.Fallida:
                meta.AplicarTransicionesDelSistema(avance: 0, Vencida);
                break;
        }

        Assert.Equal(estado, meta.Estado);
        return meta;
    }
}
