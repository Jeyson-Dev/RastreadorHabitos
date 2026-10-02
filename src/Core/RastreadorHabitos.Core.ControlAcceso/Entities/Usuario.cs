namespace RastreadorHabitos.Core.ControlAcceso.Entities;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Se guarda normalizado (sin espacios y en minúsculas) para que la unicidad no dependa del formato.
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Todo usuario tiene exactamente un rol [RF-CA-04].
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    // Conceptos independientes: el login exige ambos en true.
    // CorreoConfirmado pasa a true una sola vez, al abrir el enlace de activación [RF-CA-15, RF-CA-16].
    // CuentaHabilitada solo la cambia un Administrador [RF-CA-20].
    public bool CorreoConfirmado { get; set; } = false;
    public bool CuentaHabilitada { get; set; } = true;

    // Bloqueo temporal tras intentos fallidos consecutivos [RF-CA-19].
    public int IntentosFallidosConsecutivos { get; set; } = 0;
    public DateTime? BloqueadoHastaUtc { get; set; }

    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
}
