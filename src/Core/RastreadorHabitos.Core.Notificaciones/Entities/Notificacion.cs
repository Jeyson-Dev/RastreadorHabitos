using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Core.Notificaciones.Entities;

// Aviso interno para un usuario [RF-NOT-04]: destinatario, tipo, mensaje, leída o no y fecha.
// Quién es el destinatario lo decide quien notifica; esta pieza solo guarda su Id, sin relación
// con las tablas de Control de acceso.
public class Notificacion
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DestinatarioId { get; private set; }

    // Lo define quien notifica (por ejemplo SolicitudPermisoCreada o MetaCambioEstado): esta
    // pieza no conoce los eventos de las demás.
    public string Tipo { get; private set; } = string.Empty;

    public string Mensaje { get; private set; } = string.Empty;
    public bool Leida { get; private set; }
    public DateTime FechaUtc { get; private set; }
    public DateTime? FechaLecturaUtc { get; private set; }

    // Para EF Core.
    private Notificacion()
    {
    }

    public Notificacion(Guid destinatarioId, string tipo, string mensaje, DateTime ahoraUtc)
    {
        DestinatarioId = destinatarioId;
        Tipo = tipo;
        Mensaje = mensaje;
        FechaUtc = ahoraUtc;
    }

    // Solo su destinatario la marca [RF-NOT-05]. Marcarla otra vez no cambia nada.
    public void MarcarLeida(Guid usuarioId, DateTime ahoraUtc)
    {
        if (usuarioId != DestinatarioId)
        {
            throw new RechazoNotificacionesException(TipoRechazoNotificaciones.NoPermitido,
                "No puede marcar como leída la notificación de otro usuario.");
        }

        if (Leida)
        {
            return;
        }

        Leida = true;
        FechaLecturaUtc = ahoraUtc;
    }
}
