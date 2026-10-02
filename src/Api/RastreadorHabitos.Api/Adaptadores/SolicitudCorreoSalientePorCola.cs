using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Api.Adaptadores;

// Conecta el puerto de Control de acceso con la cola de Notificaciones. Sin lógica propia:
// Api es la única pieza que conoce a ambas.
public class SolicitudCorreoSalientePorCola : ISolicitudCorreoSaliente
{
    private readonly IColaCorreos _cola;

    public SolicitudCorreoSalientePorCola(IColaCorreos cola)
    {
        _cola = cola;
    }

    public Task SolicitarEnvioAsync(string destinatario, string asunto, string cuerpoHtml,
                                    CancellationToken cancellationToken = default)
        => _cola.EncolarAsync(destinatario, asunto, cuerpoHtml, cancellationToken);
}
