using RastreadorHabitos.Modulo.Metas.Entities;

namespace RastreadorHabitos.Modulo.Metas.Reglas;

// Evento propio del módulo que dispara una notificación [RF-NOT-03, RF-NEG-07]: una meta cambió
// de estado, por el sistema (primer cumplimiento, vencimiento) o por su dueño (completar,
// reactivar). Se avisa al dueño de la meta.
public static class AvisosMeta
{
    public const string TipoCambioEstado = "MetaCambioEstado";

    public static string MensajeCambioEstado(Meta meta, EstadoMeta anterior) =>
        $"Su meta «{meta.Nombre}» cambió de estado: estaba en {Nombre(anterior)} y ahora está en {Nombre(meta.Estado)}.";

    private static string Nombre(EstadoMeta estado) => estado switch
    {
        EstadoMeta.EnProgreso => "En progreso",
        _ => estado.ToString()
    };
}
