namespace RastreadorHabitos.Core.ControlAcceso.DTOs;

// Datos del usuario autenticado y su rol [RF-CA-07]. Nunca incluye hashes, tokens ni Ids internos.
public class UsuarioAutenticadoDto
{
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
