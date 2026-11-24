namespace RastreadorHabitos.Core.ControlAcceso.DTOs;

// Datos del usuario autenticado, su rol [RF-CA-07] y los permisos que tiene aplicados. Nunca incluye
// hashes ni tokens. El Id es el mismo que ya muestran el listado de usuarios y la auditoría; el frontend
// lo usa para no ofrecer acciones sobre uno mismo (el servidor igual las rechaza).
public class UsuarioAutenticadoDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public List<string> Permisos { get; set; } = [];
}
