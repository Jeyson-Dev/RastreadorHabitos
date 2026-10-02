namespace RastreadorHabitos.Core.ControlAcceso.Entities;

public class SesionUsuario
{
    // Este Id viaja dentro de la credencial de sesión (JWT): en cada petición se busca la
    // sesión y se comprueba que siga abierta.
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTime FechaInicioUtc { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracionUtc { get; set; }

    // Se llena al cerrar sesión [RF-CA-18]; más adelante también al cambiar la contraseña
    // [RF-CA-12] o al desactivar al usuario [RF-CA-20], que cierran todas sus sesiones.
    public DateTime? FechaCierreUtc { get; set; }

    public bool EstaAbierta(DateTime ahoraUtc) =>
        FechaCierreUtc is null && FechaExpiracionUtc > ahoraUtc;
}
