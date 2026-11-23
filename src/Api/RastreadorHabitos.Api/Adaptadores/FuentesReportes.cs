using RastreadorHabitos.Core.GestionPermisos.Services;
using RastreadorHabitos.Core.Reportes.Reglas;
using RastreadorHabitos.Core.Reportes.Services;
using RastreadorHabitos.Modulo.Habitos.Services;

namespace RastreadorHabitos.Api.Adaptadores;

// Reporte del Core [RF-REP-01]: Gestión de permisos cuenta en SQL sus solicitudes por mes y por
// estado (contarSolicitudesPorEstado) y Reportes las recibe ya agrupadas. Sin lógica propia: solo
// traduce el rango a instantes UTC y cada mes a una fila.
public class FuenteSolicitudesPorEstado : IFuenteReporte
{
    private readonly ISolicitudesPermisoService _solicitudes;
    private readonly TimeProvider _reloj;

    public FuenteSolicitudesPorEstado(ISolicitudesPermisoService solicitudes, TimeProvider reloj)
    {
        _solicitudes = solicitudes;
        _reloj = reloj;
    }

    public string Nombre => "solicitudes-por-estado";

    public string Pregunta => "¿Cuántas solicitudes de permiso hubo en cada estado, mes por mes?";

    public async Task<IReadOnlyList<FilaReporte>> CalcularAsync(Guid? usuarioId, RangoFechas rango, TimeZoneInfo zona)
    {
        // La diferencia con UTC de hoy (la zona del servidor, sin horario de verano en
        // America/Santo_Domingo), para que el mes de cada solicitud sea el local.
        var desplazamiento = (int)zona.GetUtcOffset(_reloj.GetUtcNow()).TotalMinutes;

        var meses = await _solicitudes.ContarSolicitudesPorEstadoAsync(
            usuarioId, rango.DesdeUtc(zona), rango.HastaExclusivoUtc(zona), desplazamiento);

        return meses.Select(m => new FilaReporte($"{m.Anio:D4}-{m.Mes:D2}", new Dictionary<string, decimal>
        {
            ["Pendiente"] = m.Pendiente,
            ["Aprobada"] = m.Aprobada,
            ["Aplicada"] = m.Aplicada,
            ["Rechazada"] = m.Rechazada,
            ["Total"] = m.Total
        })).ToList();
    }
}

// Reporte del negocio [RF-REP-02]: el módulo de Hábitos calcula su cumplimiento semanal sobre sus
// tablas y Reportes lo recibe ya agregado, sin conocer qué es un hábito.
public class FuenteCumplimientoSemanal : IFuenteReporte
{
    private readonly IMetricasHabitos _metricas;

    public FuenteCumplimientoSemanal(IMetricasHabitos metricas)
    {
        _metricas = metricas;
    }

    public string Nombre => "cumplimiento-semanal";

    public string Pregunta => "¿Qué porcentaje de los días programados de sus hábitos cumplió cada semana?";

    public async Task<IReadOnlyList<FilaReporte>> CalcularAsync(Guid? usuarioId, RangoFechas rango, TimeZoneInfo zona)
    {
        var semanas = await _metricas.CumplimientoSemanalAsync(usuarioId, rango.Desde, rango.Hasta);

        return semanas.Select(s => new FilaReporte(s.Semana.ToString(RangoFechas.Formato), new Dictionary<string, decimal>
        {
            ["diasProgramados"] = s.DiasProgramados,
            ["diasCumplidos"] = s.DiasCumplidos,
            ["porcentajeCumplimiento"] = s.PorcentajeCumplimiento
        })).ToList();
    }
}
