using RastreadorHabitos.Modulo.Metas.Reglas;

namespace RastreadorHabitos.Modulo.Metas.Entities;

// Entidad central del módulo de negocio, con su atributo de estado [RF-NEG-03].
// Se cumple al sumar ObjetivoCumplimientos antes de que termine el día de FechaLimite.
public class Meta
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Dueño de la meta: solo el Id que entrega Control de acceso; el negocio no conoce sus tablas.
    public Guid UsuarioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public int ObjetivoCumplimientos { get; set; }
    public DateOnly FechaLimite { get; set; }

    // Solo cambia por CambiarEstado, que respeta las transiciones declaradas en TransicionesMeta.
    public EstadoMeta Estado { get; private set; } = TransicionesMeta.EstadoInicial;

    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;

    public void CambiarEstado(EstadoMeta nuevo)
    {
        if (!TransicionesMeta.EsPermitida(Estado, nuevo))
        {
            throw new InvalidOperationException($"Transición de meta prohibida: {Estado} → {nuevo}.");
        }

        Estado = nuevo;
    }
}
