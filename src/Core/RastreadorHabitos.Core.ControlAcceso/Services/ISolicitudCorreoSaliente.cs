namespace RastreadorHabitos.Core.ControlAcceso.Services;

// Puerto de salida: Control de acceso declara qué necesita (que un correo llegue a alguien)
// sin saber quién lo resuelve. Api lo conecta con la cola de Notificaciones, así esta pieza
// no referencia a Notificaciones.
public interface ISolicitudCorreoSaliente
{
    Task SolicitarEnvioAsync(string destinatario, string asunto, string cuerpoHtml,
                             CancellationToken cancellationToken = default);
}
