namespace RastreadorHabitos.Core.GestionPermisos.Services;

// Puerto de salida hacia Notificaciones (flecha «Avisa al solicitante» del diseño). Gestión de
// permisos decide a quién avisar y qué decirle; quiénes son los Administradores activos lo sabe
// Control de acceso, y la Api conecta ambas cosas con notificar. Así esta pieza no autentica ni
// decide identidad.
public interface IAvisoSolicitudes
{
    // A cada Administrador activo, salvo a excluidoId (quien pidió no se avisa a sí mismo)
    // [RF-NOT-01].
    Task AvisarAdministradoresAsync(string tipo, string mensaje, Guid excluidoId);

    // Al usuario indicado [RF-NOT-02].
    Task AvisarUsuarioAsync(Guid usuarioId, string tipo, string mensaje);
}

// Sin avisos: para usar la pieza sola, como en sus pruebas de las Prácticas 2 y 3.
public sealed class SinAvisoSolicitudes : IAvisoSolicitudes
{
    public static readonly SinAvisoSolicitudes Instancia = new();

    public Task AvisarAdministradoresAsync(string tipo, string mensaje, Guid excluidoId) => Task.CompletedTask;

    public Task AvisarUsuarioAsync(Guid usuarioId, string tipo, string mensaje) => Task.CompletedTask;
}
