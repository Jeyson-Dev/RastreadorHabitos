namespace RastreadorHabitos.Core.Notificaciones.Services;

public interface IEnviadorColaCorreos
{
    // Hace un intento por cada correo Pendiente, marca cada uno como Enviado o suma su intento
    // fallido, y termina [RF-NOT-09, RF-NOT-10]. Nunca envía un correo Enviado o Fallido
    // [RF-NOT-12].
    Task<ResultadoEnvioCorreos> EnviarPendientesAsync(CancellationToken cancellationToken = default);
}

// Resumen de una ejecución del procesador.
// - SmtpConfigurado: sin servidor configurado no se intentó nada.
// - OtroProcesadorEnCurso: otro procesador tenía la cola y este no envió nada.
// - ConError: correos cuyo intento falló en esta ejecución; PasaronAFallido: los que con ese
//   intento agotaron el máximo.
public record ResultadoEnvioCorreos(
    bool SmtpConfigurado,
    bool OtroProcesadorEnCurso,
    int Enviados,
    int ConError,
    int PasaronAFallido,
    int Pendientes,
    string? UltimoError)
{
    public bool Exito => SmtpConfigurado && ConError == 0;
}
