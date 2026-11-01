namespace RastreadorHabitos.Core.ControlAcceso.Entities;

// Permiso concedido a un usuario, además de su rol. Control de acceso solo lo guarda y lo
// consulta: cuándo se concede lo decide Gestión de permisos, que pide aplicarlo al aprobar una
// solicitud.
public class PermisoUsuario
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    // Código del permiso, por ejemplo LIMITE_HABITOS_AMPLIADO. Se guarda en mayúsculas.
    public string Codigo { get; set; } = string.Empty;

    public DateTime FechaConcesionUtc { get; set; } = DateTime.UtcNow;
}
