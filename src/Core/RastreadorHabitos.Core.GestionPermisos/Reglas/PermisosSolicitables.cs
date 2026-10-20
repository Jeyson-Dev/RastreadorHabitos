namespace RastreadorHabitos.Core.GestionPermisos.Reglas;

// Permisos que se pueden pedir en una SolicitudPermiso. Control de acceso solo tiene roles, así
// que el único permiso solicitable es el de Administrador: al aplicarse, el solicitante puede
// ejecutar las operaciones reservadas a ese rol [RF-GP-05].
public static class PermisosSolicitables
{
    public const string Administrador = "Administrador";

    private static readonly string[] Todos = [Administrador];

    // Devuelve el nombre canónico del permiso, o null si no es uno de los solicitables.
    public static string? Normalizar(string? permiso) =>
        Todos.FirstOrDefault(p => string.Equals(p, permiso?.Trim(), StringComparison.OrdinalIgnoreCase));
}
