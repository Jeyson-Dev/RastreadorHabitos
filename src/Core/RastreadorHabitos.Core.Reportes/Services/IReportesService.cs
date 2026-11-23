using RastreadorHabitos.Core.Reportes.DTOs;

namespace RastreadorHabitos.Core.Reportes.Services;

// Reportes con agregación y filtro por rol. Esta pieza no autentica: la Api le pasa el Id y el
// rol vigente del usuario de la sesión, que Control de acceso ya verificó.
public interface IReportesService
{
    // Un Estándar solo obtiene sus propios datos; un Administrador, los de todos [RF-REP-04]. El
    // rango llega como texto de la consulta y se valida aquí [RF-REP-05, RD-07].
    Task<ReporteDto> GenerarAsync(string reporte, Guid consultanteId, bool esAdministrador,
                                  string? desde, string? hasta);
}
