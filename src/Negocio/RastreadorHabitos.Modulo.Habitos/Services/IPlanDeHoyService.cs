using RastreadorHabitos.Modulo.Habitos.DTOs;

namespace RastreadorHabitos.Modulo.Habitos.Services;

// Plan de hoy del usuario que Control de acceso ya identificó. Usa datos del servicio de clima
// externo; si no está disponible, la operación se rechaza de forma controlada y el resto del
// módulo sigue funcionando.
public interface IPlanDeHoyService
{
    Task<PlanDeHoyDto> ObtenerAsync(Guid usuarioId, string? ciudad, CancellationToken cancelacion = default);
}
