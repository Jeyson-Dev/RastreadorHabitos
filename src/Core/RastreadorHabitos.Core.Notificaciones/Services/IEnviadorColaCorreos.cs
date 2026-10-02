namespace RastreadorHabitos.Core.Notificaciones.Services;

public interface IEnviadorColaCorreos
{
    // Envía por SMTP los correos pendientes, los marca como enviados y termina [RF-NOT-09].
    Task<ResultadoEnvioCorreos> EnviarPendientesAsync();
}

public record ResultadoEnvioCorreos(bool SmtpConfigurado, int Enviados, int Pendientes, string? MotivoFallo)
{
    public bool Exito => SmtpConfigurado && MotivoFallo is null;
}
