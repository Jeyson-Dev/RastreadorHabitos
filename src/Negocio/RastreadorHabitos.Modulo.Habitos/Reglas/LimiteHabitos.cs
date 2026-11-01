namespace RastreadorHabitos.Modulo.Habitos.Reglas;

// Cuántos hábitos puede tener un usuario: 10, o 20 si Control de acceso le concedió el permiso
// LIMITE_HABITOS_AMPLIADO (lo concede Gestión de permisos al aprobar su solicitud).
public static class LimiteHabitos
{
    public const int Normal = 10;
    public const int Ampliado = 20;

    // Mismo código que el catálogo de permisos solicitables de Gestión de permisos.
    public const string PermisoAmpliado = "LIMITE_HABITOS_AMPLIADO";
}
