using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Reglas;

namespace RastreadorHabitos.Core.GestionPermisos.Services;

public class SolicitudesPermisoService : ISolicitudesPermisoService
{
    private readonly GestionPermisosDbContext _context;
    private readonly IAplicadorPermiso _aplicador;

    public SolicitudesPermisoService(GestionPermisosDbContext context, IAplicadorPermiso aplicador)
    {
        _context = context;
        _aplicador = aplicador;
    }

    public async Task<SolicitudPermisoDto> CrearAsync(Guid solicitanteId, CreacionSolicitudPermisoDto solicitud)
    {
        // Entrada inválida: rechazo controlado, nunca una excepción sin manejar [RD-07].
        var permiso = PermisosSolicitables.Normalizar(solicitud.Permiso)
            ?? throw new RechazoGestionPermisosException(TipoRechazoGestionPermisos.DatosInvalidos,
                $"El permiso solicitado debe ser {PermisosSolicitables.Administrador}.");

        var nueva = new SolicitudPermiso(solicitanteId, permiso, DateTime.UtcNow);
        _context.SolicitudesPermiso.Add(nueva);
        await _context.SaveChangesAsync();

        return SolicitudPermisoDto.Desde(nueva);
    }

    public async Task<IReadOnlyList<SolicitudPermisoDto>> ListarPendientesAsync()
    {
        var pendientes = await _context.SolicitudesPermiso
            .Where(s => s.Estado == EstadoSolicitud.Pendiente)
            .OrderBy(s => s.FechaCreacionUtc)
            .ToListAsync();

        return pendientes.Select(SolicitudPermisoDto.Desde).ToList();
    }

    public async Task<SolicitudPermisoDto> AprobarAsync(Guid solicitudId, Guid administradorId)
    {
        var solicitud = await BuscarAsync(solicitudId);

        solicitud.Aprobar(administradorId, DateTime.UtcNow);

        // Sin intervención manual: Control de acceso aplica el permiso y la solicitud queda
        // Aplicada. Se guarda todo al final; si la aplicación falla, la solicitud sigue Pendiente.
        await _aplicador.AplicarAsync(solicitud.SolicitanteId, solicitud.Permiso, administradorId);
        solicitud.MarcarAplicada();

        await _context.SaveChangesAsync();
        return SolicitudPermisoDto.Desde(solicitud);
    }

    public async Task<SolicitudPermisoDto> RechazarAsync(Guid solicitudId, Guid administradorId,
                                                          RechazoSolicitudPermisoDto rechazo)
    {
        var solicitud = await BuscarAsync(solicitudId);

        solicitud.Rechazar(administradorId, rechazo.Motivo, DateTime.UtcNow);

        await _context.SaveChangesAsync();
        return SolicitudPermisoDto.Desde(solicitud);
    }

    private async Task<SolicitudPermiso> BuscarAsync(Guid solicitudId) =>
        await _context.SolicitudesPermiso.FindAsync(solicitudId)
        ?? throw new RechazoGestionPermisosException(TipoRechazoGestionPermisos.NoEncontrado,
            "No existe una solicitud con ese id.");
}
