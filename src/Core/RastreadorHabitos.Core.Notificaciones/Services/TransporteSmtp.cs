using System.Net;
using System.Net.Mail;
using RastreadorHabitos.Core.Notificaciones.Configuracion;

namespace RastreadorHabitos.Core.Notificaciones.Services;

// Envía por SMTP con los datos de las variables de entorno Smtp__* [RF-NOT-13, RD-10]. Nunca
// escribe la contraseña en un mensaje ni en el log.
public class TransporteSmtp : ITransporteCorreo
{
    private readonly OpcionesSmtp _opciones;

    public TransporteSmtp(OpcionesSmtp opciones)
    {
        _opciones = opciones;
    }

    public bool EstaConfigurado => _opciones.EstaConfigurado;

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken)
    {
        // Un cliente por correo: si un envío se cancela por tiempo, el cliente queda inservible.
        using var cliente = new SmtpClient(_opciones.Host, _opciones.Puerto!.Value)
        {
            EnableSsl = _opciones.UsarSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
        {
            cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Contrasena);
        }

        MailMessage mensaje;
        try
        {
            mensaje = new MailMessage(_opciones.Remitente!, destinatario, asunto, cuerpoHtml) { IsBodyHtml = true };
        }
        // MailMessage rechaza un destinatario mal formado con FormatException y uno vacío con
        // ArgumentException: los dos son un destinatario inválido, no un error del procesador
        // [Hallazgo 1 de la Asignación 3, RF-NOT-10].
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            throw new FalloEnvioCorreoException("La dirección del destinatario no es válida.");
        }

        using (mensaje)
        {
            try
            {
                await cliente.SendMailAsync(mensaje, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SmtpFailedRecipientException ex)
            {
                throw new FalloEnvioCorreoException($"El servidor de correo rechazó al destinatario: {MensajeMasInterno(ex)}");
            }
            catch (Exception ex)
            {
                // Servidor caído o inalcanzable, TLS, credenciales rechazadas: todo cuenta como un
                // intento fallido del correo, nunca detiene al procesador.
                throw new FalloEnvioCorreoException($"No se pudo enviar por el servidor de correo: {MensajeMasInterno(ex)}");
            }
        }
    }

    // El motivo concreto (por ejemplo «Connection refused» o «No such host is known») suele estar
    // en la excepción más interna. Solo el mensaje: nunca la traza [RD-08].
    private static string MensajeMasInterno(Exception ex)
    {
        while (ex.InnerException is not null)
        {
            ex = ex.InnerException;
        }
        return ex.Message.TrimEnd('.') + ".";
    }
}
