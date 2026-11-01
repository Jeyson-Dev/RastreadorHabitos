using RastreadorHabitos.Modulo.Metas.Reglas;

namespace RastreadorHabitos.Modulo.Metas.Entities;

// Entidad central del módulo de negocio, con su atributo de estado [RF-NEG-03]. Agrupa hábitos
// del usuario y su avance es la cantidad de días cumplidos de esos hábitos entre FechaInicio y
// FechaLimite. El avance no se guarda: lo calcula Hábitos cada vez.
public class Meta
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Dueño de la meta: solo el Id que entrega Control de acceso; el negocio no conoce sus tablas.
    public Guid UsuarioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public int ObjetivoCumplimientos { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaLimite { get; set; }

    // Solo cambia por CambiarEstado, que resuelve la transición en TransicionesMeta [RD-04].
    public EstadoMeta Estado { get; private set; } = TransicionesMeta.EstadoInicial;

    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;

    public List<MetaHabito> Habitos { get; set; } = [];

    // Si la transición no está permitida, no la ejecuta quien corresponde o no se cumple su
    // condición, se rechaza y el estado no cambia [RF-NEG-04].
    public void CambiarEstado(EstadoMeta hacia, ContextoTransicion contexto)
    {
        TransicionesMeta.Validar(this, hacia, contexto);
        Estado = hacia;
    }

    // El sistema aplica, en el orden de la tabla, las transiciones suyas cuya condición se
    // cumple: el primer cumplimiento pasa la meta a En progreso, y vencer sin alcanzar el
    // objetivo la deja Fallida.
    public void AplicarTransicionesDelSistema(int avance, DateOnly hoy)
    {
        var contexto = new ContextoTransicion(EjecutorTransicion.Sistema, avance, hoy);
        for (var paso = 0; paso < Enum.GetValues<EstadoMeta>().Length; paso++)
        {
            if (TransicionesMeta.SiguienteDelSistema(this, contexto) is not { } siguiente)
            {
                return;
            }
            CambiarEstado(siguiente, contexto);
        }
    }

    // El dueño cierra la meta como Completada.
    public void Completar(int avance, DateOnly hoy) =>
        CambiarEstado(EstadoMeta.Completada, new ContextoTransicion(EjecutorTransicion.Dueno, avance, hoy));

    // El dueño reactiva una meta Fallida con una fecha límite nueva.
    public void Reactivar(DateOnly nuevaFechaLimite, int avance, DateOnly hoy)
    {
        CambiarEstado(EstadoMeta.EnProgreso,
            new ContextoTransicion(EjecutorTransicion.Dueno, avance, hoy, nuevaFechaLimite));
        FechaLimite = nuevaFechaLimite;
    }
}
