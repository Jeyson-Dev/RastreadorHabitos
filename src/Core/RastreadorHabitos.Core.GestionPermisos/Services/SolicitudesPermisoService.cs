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
    private readonly IAvisoSolicitudes _aviso;

    // Sin aviso, la pieza trabaja sola (sus pruebas); la Api siempre conecta el de Notificaciones.
    public SolicitudesPermisoService(GestionPermisosDbContext context, IAplicadorPermiso aplicador,
                                     IAvisoSolicitudes? aviso = null)
    {
        _context = context;
        _aplicador = aplicador;
        _aviso = aviso ?? SinAvisoSolicitudes.Instancia;
    }

    public async Task<SolicitudPermisoDto> CrearAsync(Guid solicitanteId, CreacionSolicitudPermisoDto solicitud)
    {
        // Entrada inválida: rechazo controlado, nunca una excepción sin manejar [RD-07].
        var permiso = PermisosSolicitables.Normalizar(solicitud.Permiso)
            ?? throw new RechazoGestionPermisosException(TipoRechazoGestionPermisos.DatosInvalidos,
                $"El permiso solicitado debe ser {string.Join(" o ", PermisosSolicitables.Todos)}.");

        var nueva = new SolicitudPermiso(solicitanteId, permiso, DateTime.UtcNow);
        _context.SolicitudesPermiso.Add(nueva);
        await _context.SaveChangesAsync();

        // Ya guardada, se avisa a quienes pueden resolverla: los Administradores activos, salvo el
        // propio solicitante, que no puede resolver su solicitud [RF-NOT-01, RF-GP-07].
        await _aviso.AvisarAdministradoresAsync(AvisosSolicitud.TipoCreada, AvisosSolicitud.MensajeCreada(nueva),
                                                excluidoId: solicitanteId);

        return SolicitudPermisoDto.Desde(nueva);
    }

    public async Task<IReadOnlyList<SolicitudPermisoDto>> ListarPendientesAsync(Guid consultanteId, bool esAdministrador)
    {
        var consulta = _context.SolicitudesPermiso.Where(s => s.Estado == EstadoSolicitud.Pendiente);

        // Un Estándar solo ve las suyas, nunca las de otros usuarios [RF-GP-02].
        if (!esAdministrador)
        {
            consulta = consulta.Where(s => s.SolicitanteId == consultanteId);
        }

        var pendientes = await consulta
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

        // Solo después de confirmar: una aprobación que no ocurrió no se avisa [RF-NOT-02].
        await _aviso.AvisarUsuarioAsync(solicitud.SolicitanteId, AvisosSolicitud.TipoAprobada,
                                        AvisosSolicitud.MensajeAprobada(solicitud));
        return SolicitudPermisoDto.Desde(solicitud);
    }

    public async Task<SolicitudPermisoDto> RechazarAsync(Guid solicitudId, Guid administradorId,
                                                          RechazoSolicitudPermisoDto rechazo)
    {
        var solicitud = await BuscarAsync(solicitudId);

        solicitud.Rechazar(administradorId, rechazo.Motivo, DateTime.UtcNow);

        await GuardarAsync();

        // El solicitante sabe que se rechazó y por qué [RF-NOT-02].
        await _aviso.AvisarUsuarioAsync(solicitud.SolicitanteId, AvisosSolicitud.TipoRechazada,
                                        AvisosSolicitud.MensajeRechazada(solicitud));
        return SolicitudPermisoDto.Desde(solicitud);
    }

    public async Task<IReadOnlyList<ConteoSolicitudesDto>> ContarSolicitudesPorEstadoAsync(
        Guid? solicitanteId, IReadOnlyList<PeriodoUtc> periodos)
    {
        var conteos = new List<ConteoSolicitudesDto>(periodos.Count);
        foreach (var periodo in periodos)
        {
            // Una consulta GROUP BY Estado por intervalo: SQL Server cuenta; nunca se traen las
            // solicitudes para contarlas aquí [RF-REP-03]. Los límites de cada intervalo los
            // decide quien pide el reporte, en UTC, así el mes es el local de cada fecha aunque la
            // zona tenga horario de verano [Hallazgo 3 de la Asignación 3].
            var porEstado = await Filtrar(solicitanteId, periodo.DesdeUtc, periodo.HastaUtc)
                .GroupBy(s => s.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(g => g.Estado, g => g.Cantidad);

            conteos.Add(new ConteoSolicitudesDto
            {
                Pendiente = porEstado.GetValueOrDefault(EstadoSolicitud.Pendiente),
                Aprobada = porEstado.GetValueOrDefault(EstadoSolicitud.Aprobada),
                Aplicada = porEstado.GetValueOrDefault(EstadoSolicitud.Aplicada),
                Rechazada = porEstado.GetValueOrDefault(EstadoSolicitud.Rechazada),
                Total = porEstado.Values.Sum()
            });
        }
        return conteos;
    }

    public async Task<(DateTime PrimeraUtc, DateTime UltimaUtc)?> FechasDeCreacionExtremasAsync(
        Guid? solicitanteId, DateTime? desdeUtc, DateTime? hastaUtc)
    {
        var consulta = Filtrar(solicitanteId, desdeUtc, hastaUtc);
        var primera = await consulta.MinAsync(s => (DateTime?)s.FechaCreacionUtc);
        var ultima = await consulta.MaxAsync(s => (DateTime?)s.FechaCreacionUtc);
        return primera is { } p && ultima is { } u ? (p, u) : null;
    }

    private IQueryable<SolicitudPermiso> Filtrar(Guid? solicitanteId, DateTime? desdeUtc, DateTime? hastaUtc)
    {
        var consulta = _context.SolicitudesPermiso.AsNoTracking();
        if (solicitanteId is { } id)
        {
            consulta = consulta.Where(s => s.SolicitanteId == id);
        }
        if (desdeUtc is { } desde)
        {
            consulta = consulta.Where(s => s.FechaCreacionUtc >= desde);
        }
        if (hastaUtc is { } hasta)
        {
            consulta = consulta.Where(s => s.FechaCreacionUtc < hasta);
        }
        return consulta;
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
