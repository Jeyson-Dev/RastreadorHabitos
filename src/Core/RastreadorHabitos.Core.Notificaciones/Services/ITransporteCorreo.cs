namespace RastreadorHabitos.Core.Notificaciones.Services;

// Cómo sale un correo del sistema. El procesador de la cola no conoce SMTP: así se prueba sin un
// servidor de correo [RD-12]. La implementación real es TransporteSmtp.
public interface ITransporteCorreo
{
    // Sin servidor configurado no se intenta nada: los correos esperan en la cola.
    bool EstaConfigurado { get; }

    // Un fallo esperado (servidor caído, destinatario inválido, rechazo del servidor) se informa
    // con FalloEnvioCorreoException y un mensaje comprensible, sin trazas [RD-08].
    Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken);
}

public class FalloEnvioCorreoException : Exception
{
    public FalloEnvioCorreoException(string mensaje) : base(mensaje)
    {
    }
}
