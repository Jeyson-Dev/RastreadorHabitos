namespace RastreadorHabitos.Core.ControlAcceso.DTOs;

// Fila del listado de usuarios para el Administrador [RF-CA-21]. Nunca incluye hashes ni tokens.
public class UsuarioListadoDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
