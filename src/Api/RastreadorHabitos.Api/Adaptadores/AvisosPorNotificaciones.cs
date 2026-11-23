using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.GestionPermisos.Services;
using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Api.Adaptadores;

// Conecta el puerto de Notificaciones que pide el correo del destinatario con Control de acceso.
// Sin lógica propia: la Api es la única pieza que conoce a ambas.
public class DirectorioDestinatariosPorControlAcceso : IDirectorioDestinatarios
{
    private readonly IDirectorioUsuarios _usuarios;

    public DirectorioDestinatariosPorControlAcceso(IDirectorioUsuarios usuarios)
    {
        _usuarios = usuarios;
    }

    public Task<string?> ObtenerCorreoAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        => _usuarios.ObtenerCorreoAsync(usuarioId, cancellationToken);
}

// Conecta el puerto de avisos de Gestión de permisos con notificar. Quiénes son los
// Administradores activos lo responde Control de acceso en el momento del aviso [RF-NOT-01];
// qué se avisa y a quién lo decidió Gestión de permisos.
public class AvisoSolicitudesPorNotificaciones : IAvisoSolicitudes
{
    private readonly IDirectorioUsuarios _usuarios;
    private readonly INotificador _notificador;

    public AvisoSolicitudesPorNotificaciones(IDirectorioUsuarios usuarios, INotificador notificador)
    {
        _usuarios = usuarios;
        _notificador = notificador;
    }

    public async Task AvisarAdministradoresAsync(string tipo, string mensaje, Guid excluidoId)
    {
        foreach (var administradorId in await _usuarios.ListarAdministradoresActivosAsync())
        {
            if (administradorId != excluidoId)
            {
                await _notificador.NotificarAsync(administradorId, tipo, mensaje);
            }
        }
    }

    public Task AvisarUsuarioAsync(Guid usuarioId, string tipo, string mensaje)
        => _notificador.NotificarAsync(usuarioId, tipo, mensaje);
}
