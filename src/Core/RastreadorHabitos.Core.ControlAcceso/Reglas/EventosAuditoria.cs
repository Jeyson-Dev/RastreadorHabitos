namespace RastreadorHabitos.Core.ControlAcceso.Reglas;

// Qué audita Control de acceso, en un solo lugar [RF-AUD-04, RF-AUD-05]. Los valores anterior y
// nuevo llevan solo lo que cambia y nunca la contraseña, su hash, un token ni un código.
public static class EventosAuditoria
{
    public const string Entidad = "Usuario";

    public const string RolCambiado = "RolCambiado";
    public const string UsuarioDesactivado = "UsuarioDesactivado";
    public const string UsuarioReactivado = "UsuarioReactivado";

    // Quién lo inició queda en la acción, en el usuario que actuó y en el campo iniciadoPor.
    public const string RestablecimientoPropio = "RestablecimientoContrasenaPropio";
    public const string RestablecimientoForzado = "RestablecimientoContrasenaForzado";

    public const string IniciadoPorUsuario = "Usuario";
    public const string IniciadoPorAdministrador = "Administrador";
}
