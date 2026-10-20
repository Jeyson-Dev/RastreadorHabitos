using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Reglas;

namespace RastreadorHabitos.Core.GestionPermisos.Services;

public class SolicitudesPermisoService : ISolicitudesPermisoService
{
    private readonly GestionPermisosDbContext _context;

    public SolicitudesPermisoService(GestionPermisosDbContext context)
    {
        _context = context;
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
}
