namespace RastreadorHabitos.Core.ControlAcceso.Entities;

public class Rol
{
    public const int IdAdministrador = 1;
    public const int IdEstandar = 2;

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
