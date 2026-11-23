using RastreadorHabitos.Core.Notificaciones.Reglas;

namespace RastreadorHabitos.Core.Notificaciones.Entities;

// Correo saliente: las operaciones solo lo encolan [RF-NOT-08] y el procesador de la cola lo
// envía [RF-NOT-09]. Registra su estado, intentos, fechas y último error [RF-NOT-11].
public class CorreoEnCola
{
    public const string EstadoPendiente = "Pendiente";
    public const string EstadoEnviado = "Enviado";
    public const string EstadoFallido = "Fallido";

    public static readonly IReadOnlyList<string> Estados = [EstadoPendiente, EstadoEnviado, EstadoFallido];

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string CuerpoHtml { get; set; } = string.Empty;
    public string Estado { get; set; } = EstadoPendiente;
    public int Intentos { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FechaUltimoIntentoUtc { get; set; }
    public DateTime? FechaEnvioUtc { get; set; }

    // Qué falló en el último intento, en texto comprensible y sin trazas [RD-08].
    public string? UltimoError { get; set; }

    // Un intento que salió: queda Enviado y nunca vuelve a salir [RF-NOT-12].
    public void RegistrarEnvio(DateTime ahoraUtc)
    {
        Intentos++;
        FechaUltimoIntentoUtc = ahoraUtc;
        FechaEnvioUtc = ahoraUtc;
        Estado = EstadoEnviado;
    }

    // Un intento que falló: sigue Pendiente hasta agotar los intentos y entonces queda Fallido,
    // con su último error. Nunca se borra [RF-NOT-10].
    public void RegistrarFallo(string error, DateTime ahoraUtc)
    {
        Intentos++;
        FechaUltimoIntentoUtc = ahoraUtc;
        UltimoError = error.Length <= ReglasCola.LongitudMaximaError
            ? error
            : error[..ReglasCola.LongitudMaximaError];

        if (Intentos >= ReglasCola.MaximoIntentos)
        {
            Estado = EstadoFallido;
        }
    }
}
