namespace RastreadorHabitos.Core.ControlAcceso.DTOs;

// Credencial de sesión que entrega el inicio de sesión [RF-CA-03].
public class CredencialSesionDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraUtc { get; set; }
}
