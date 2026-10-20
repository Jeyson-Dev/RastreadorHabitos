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

        // Guardar Aprobada dentro de la transacción retiene la solicitud mientras se aplica el
        // permiso: una resolución simultánea espera y después se rechaza [RF-GP-06]. Si la
        // aplicación falla, se deshace todo y la solicitud sigue Pendiente.
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        await GuardarAsync();

        // Sin intervención manual: Control de acceso aplica el permiso y la solicitud queda Aplicada.
        await _aplicador.AplicarAsync(solicitud.SolicitanteId, solicitud.Permiso, administradorId);
        solicitud.MarcarAplicada();
        await GuardarAsync();

        await transaccion.CommitAsync();
        return SolicitudPermisoDto.Desde(solicitud);
    }

    public async Task<SolicitudPermisoDto> RechazarAsync(Guid solicitudId, Guid administradorId,
                                                          RechazoSolicitudPermisoDto rechazo)
    {
        var solicitud = await BuscarAsync(solicitudId);

        solicitud.Rechazar(administradorId, rechazo.Motivo, DateTime.UtcNow);

        await GuardarAsync();
        return SolicitudPermisoDto.Desde(solicitud);
    }

    // Si otra operación resolvió la solicitud después de leerla, el estado guardado ya no es el
    // leído: se rechaza como cualquier transición fuera de la tabla y no cambia nada [RF-GP-06].
    private async Task GuardarAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RechazoGestionPermisosException(TipoRechazoGestionPermisos.TransicionNoPermitida,
                "La solicitud ya fue resuelta por otra operación.");
        }
    }

    private async Task<SolicitudPermiso> BuscarAsync(Guid solicitudId) =>
        await _context.SolicitudesPermiso.FindAsync(solicitudId)
        ?? throw new RechazoGestionPermisosException(TipoRechazoGestionPermisos.NoEncontrado,
            "No existe una solicitud con ese id.");
}
