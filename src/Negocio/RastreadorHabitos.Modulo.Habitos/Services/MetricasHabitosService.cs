using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Habitos.Reglas;

namespace RastreadorHabitos.Modulo.Habitos.Services;

public class MetricasHabitosService : IMetricasHabitos
{
    // DateOnly.DayNumber cuenta días desde el 1 de enero del año 1, que fue lunes: dividir entre 7
    // da el número de la semana de lunes a domingo, igual en SQL Server que aquí.
    private const int DiasPorSemana = 7;

    private readonly HabitosDbContext _context;
    private readonly TimeProvider _reloj;

    public MetricasHabitosService(HabitosDbContext context, TimeProvider reloj)
    {
        _context = context;
        _reloj = reloj;
    }

    public async Task<IReadOnlyList<CumplimientoSemanalDto>> CumplimientoSemanalAsync(Guid? usuarioId,
                                                                                       DateOnly? desde, DateOnly? hasta)
    {
        // Los días que todavía no llegaron no cuentan como programados.
        var hoy = DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime);
        var fin = hasta is { } h && h < hoy ? h : hoy;

        var habitos = _context.Habitos.AsNoTracking();
        if (usuarioId is { } dueno)
        {
            habitos = habitos.Where(hb => hb.UsuarioId == dueno);
        }

        // Días cumplidos por semana: los cuenta SQL Server con GROUP BY sobre los registros
        // diarios, nunca se traen para sumarlos aquí [RF-REP-03].
        var registros = _context.RegistrosDiarios.AsNoTracking().Where(r => r.Cumplido && r.Fecha <= fin);
        if (desde is { } inicio)
        {
            registros = registros.Where(r => r.Fecha >= inicio);
        }
        if (usuarioId is not null)
        {
            registros = registros.Where(r => habitos.Any(hb => hb.Id == r.HabitoId));
        }

        var cumplidosPorSemana = await registros
            .GroupBy(r => r.Fecha.DayNumber / DiasPorSemana)
            .Select(g => new { Semana = g.Key, Cumplidos = g.Count() })
            .ToDictionaryAsync(g => g.Semana, g => g.Cumplidos);

        // Días programados por semana: salen del calendario de cada hábito (sus días de la semana
        // desde que se creó y hasta el día en que se archivó, si se archivó), no de sus registros.
        // Se leen los hábitos, no los registros.
        var calendario = await habitos
            .Select(hb => new { hb.FechaCreacionUtc, hb.FechaArchivadoUtc, Dias = hb.Dias.Select(d => d.DiaSemana).ToList() })
            .ToListAsync();

        var programadosPorSemana = new Dictionary<int, int>();
        foreach (var habito in calendario)
        {
            var creado = DiaLocal(habito.FechaCreacionUtc);
            var primero = desde is { } d && d > creado ? d : creado;
            var ultimo = habito.FechaArchivadoUtc is { } archivado && DiaLocal(archivado) < fin ? DiaLocal(archivado) : fin;
            for (var dia = primero; dia <= ultimo; dia = dia.AddDays(1))
            {
                if (habito.Dias.Contains(DiasSemana.De(dia)))
                {
                    var semana = dia.DayNumber / DiasPorSemana;
                    programadosPorSemana[semana] = programadosPorSemana.GetValueOrDefault(semana) + 1;
                }
            }
        }

        return programadosPorSemana.Keys
            .Union(cumplidosPorSemana.Keys)
            .Order()
            .Select(semana =>
            {
                var programados = programadosPorSemana.GetValueOrDefault(semana);
                var cumplidos = cumplidosPorSemana.GetValueOrDefault(semana);
                return new CumplimientoSemanalDto
                {
                    Semana = DateOnly.FromDayNumber(semana * DiasPorSemana),
                    DiasProgramados = programados,
                    DiasCumplidos = cumplidos,
                    PorcentajeCumplimiento = programados == 0 ? 0 : Math.Round(cumplidos * 100m / programados, 1)
                };
            })
            .ToList();
    }

    // El día local en que se creó el hábito, con el mismo reloj que «hoy» [RD-11].
    private DateOnly DiaLocal(DateTime fechaUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc),
                                                              _reloj.LocalTimeZone));
}
