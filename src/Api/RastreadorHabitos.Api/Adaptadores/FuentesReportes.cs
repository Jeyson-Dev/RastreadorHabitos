using RastreadorHabitos.Core.GestionPermisos.Services;
using RastreadorHabitos.Core.Reportes.Reglas;
using RastreadorHabitos.Core.Reportes.Services;
using RastreadorHabitos.Modulo.Habitos.Services;

namespace RastreadorHabitos.Api.Adaptadores;

// Reporte del Core [RF-REP-01]: Gestión de permisos cuenta en SQL sus solicitudes por estado
// (contarSolicitudesPorEstado) y Reportes las recibe ya agrupadas. Este adaptador solo decide los
// intervalos: un mes local por fila, convertido a UTC con las reglas de la zona para ESE mes.
public class FuenteSolicitudesPorEstado : IFuenteReporte
{
    private readonly ISolicitudesPermisoService _solicitudes;

    // El reloj ya no hace falta (la zona llega en cada cálculo); se conserva en la firma para no
    // cambiar el constructor público que usan la Api y las pruebas.
    public FuenteSolicitudesPorEstado(ISolicitudesPermisoService solicitudes, TimeProvider reloj)
    {
        _solicitudes = solicitudes;
        _ = reloj;
    }

    public string Nombre => "solicitudes-por-estado";

    public string Pregunta => "¿Cuántas solicitudes de permiso hubo en cada estado, mes por mes?";

    public async Task<IReadOnlyList<FilaReporte>> CalcularAsync(Guid? usuarioId, RangoFechas rango, TimeZoneInfo zona)
    {
        var desdeUtc = rango.DesdeUtc(zona);
        var hastaUtc = rango.HastaExclusivoUtc(zona);

        // Solo los meses que tienen solicitudes dentro del rango.
        if (await _solicitudes.FechasDeCreacionExtremasAsync(usuarioId, desdeUtc, hastaUtc) is not { } extremos)
        {
            return [];
        }

        // Antes se usaba la diferencia con UTC de hoy para todas las fechas: con una zona con
        // horario de verano, una solicitud del 1 de agosto a las 00:30 caía en julio. Ahora cada
        // mes local tiene sus propios límites en UTC [Hallazgo 3 de la Asignación 3, RF-REP-05].
        var meses = new List<DateOnly>();
        var periodos = new List<Core.GestionPermisos.DTOs.PeriodoUtc>();
        for (var mes = MesLocal(extremos.PrimeraUtc, zona); mes <= MesLocal(extremos.UltimaUtc, zona); mes = mes.AddMonths(1))
        {
            var inicio = RangoFechas.InicioDelDiaUtc(mes, zona);
            var fin = RangoFechas.InicioDelDiaUtc(mes.AddMonths(1), zona);
            meses.Add(mes);
            periodos.Add(new(desdeUtc is { } d && d > inicio ? d : inicio, hastaUtc is { } h && h < fin ? h : fin));
        }

        var conteos = await _solicitudes.ContarSolicitudesPorEstadoAsync(usuarioId, periodos);

        return meses.Zip(conteos)
            .Where(m => m.Second.Total > 0)
            .Select(m => new FilaReporte(m.First.ToString("yyyy-MM"), new Dictionary<string, decimal>
            {
                ["Pendiente"] = m.Second.Pendiente,
                ["Aprobada"] = m.Second.Aprobada,
                ["Aplicada"] = m.Second.Aplicada,
                ["Rechazada"] = m.Second.Rechazada,
                ["Total"] = m.Second.Total
            })).ToList();
    }

    // El primer día del mes local de un instante UTC.
    private static DateOnly MesLocal(DateTime fechaUtc, TimeZoneInfo zona)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc), zona);
        return new DateOnly(local.Year, local.Month, 1);
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
