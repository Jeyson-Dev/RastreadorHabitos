using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Notificaciones.Configuracion;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.Entities;

namespace RastreadorHabitos.Core.Notificaciones.Services;

// Lo ejecuta un comando independiente de las operaciones que originan los correos: ellas solo
// los dejan en la cola, así terminan bien aunque el servidor SMTP no responda [RF-NOT-08].
public class EnviadorColaCorreos : IEnviadorColaCorreos
{
    private const int TamanoLote = 20;
    private static readonly TimeSpan TiempoMaximoEnvio = TimeSpan.FromSeconds(10);

    private readonly NotificacionesDbContext _context;
    private readonly OpcionesSmtp _opcionesSmtp;

    public EnviadorColaCorreos(NotificacionesDbContext context, OpcionesSmtp opcionesSmtp)
    {
        _context = context;
        _opcionesSmtp = opcionesSmtp;
    }

    public async Task<ResultadoEnvioCorreos> EnviarPendientesAsync()
    {
        // Solo se toman correos Pendiente: uno ya marcado Enviado nunca vuelve a salir, por eso
        // ejecutar el comando dos veces no duplica envíos [RF-NOT-12].
        var pendientes = _context.CorreosEnCola.Where(c => c.Estado == CorreoEnCola.EstadoPendiente);

        if (!_opcionesSmtp.EstaConfigurado)
        {
            return new ResultadoEnvioCorreos(false, 0, await pendientes.CountAsync(), null);
        }

        var enviados = 0;
        string? motivoFallo = null;
        using var cliente = CrearClienteSmtp();

        while (motivoFallo is null)
        {
            var lote = await pendientes.OrderBy(c => c.FechaCreacionUtc).Take(TamanoLote).ToListAsync();
            if (lote.Count == 0)
            {
                break;
            }

            foreach (var correo in lote)
            {
                try
                {
                    using var mensaje = new MailMessage(_opcionesSmtp.Remitente!, correo.Destinatario,
                                                        correo.Asunto, correo.CuerpoHtml) { IsBodyHtml = true };
                    using var limiteEnvio = new CancellationTokenSource(TiempoMaximoEnvio);
                    await cliente.SendMailAsync(mensaje, limiteEnvio.Token);
                }
                catch (OperationCanceledException)
                {
                    motivoFallo = $"el servidor SMTP no respondió en {TiempoMaximoEnvio.TotalSeconds} segundos";
                    break;
                }
                catch (Exception ex)
                {
                    // Sigue Pendiente; si el servidor falla, no tiene sentido intentar el resto.
                    motivoFallo = ex.InnerException?.Message ?? ex.Message;
                    break;
                }

                // Se guarda después de cada envío, no al final: si el proceso se detiene, los
                // correos ya enviados no se repiten en la siguiente ejecución.
                correo.Estado = CorreoEnCola.EstadoEnviado;
                correo.FechaEnvioUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                enviados++;
            }
        }

        return new ResultadoEnvioCorreos(true, enviados, await pendientes.CountAsync(), motivoFallo);
    }

    private SmtpClient CrearClienteSmtp()
    {
        var cliente = new SmtpClient(_opcionesSmtp.Host, _opcionesSmtp.Puerto!.Value)
        {
            EnableSsl = _opcionesSmtp.UsarSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_opcionesSmtp.Usuario))
        {
            cliente.Credentials = new NetworkCredential(_opcionesSmtp.Usuario, _opcionesSmtp.Contrasena);
        }

        return cliente;
    }
}
