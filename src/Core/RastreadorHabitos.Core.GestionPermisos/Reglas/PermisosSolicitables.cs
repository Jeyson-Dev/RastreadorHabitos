namespace RastreadorHabitos.Core.GestionPermisos.Reglas;

// Permisos que se pueden pedir en una SolicitudPermiso. Al aprobarse, Control de acceso los
// aplica y el solicitante puede ejecutar la acción que pidió [RF-GP-05]:
// - Administrador: el rol de Administrador, con las operaciones reservadas a ese rol.
// - LIMITE_HABITOS_AMPLIADO: crear más hábitos activos que el límite normal del módulo de
//   negocio. Esta pieza no conoce el negocio: solo tramita el código del permiso.
public static class PermisosSolicitables
{
    public const string Administrador = "Administrador";
    public const string LimiteHabitosAmpliado = "LIMITE_HABITOS_AMPLIADO";

    public static readonly IReadOnlyList<string> Todos = [Administrador, LimiteHabitosAmpliado];

    // Devuelve el nombre canónico del permiso, o null si no es uno de los solicitables.
    public static string? Normalizar(string? permiso) =>
        Todos.FirstOrDefault(p => string.Equals(p, permiso?.Trim(), StringComparison.OrdinalIgnoreCase));
}
