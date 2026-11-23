using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Api.Adaptadores;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Services;
using RastreadorHabitos.Core.Reportes.Services;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.Services;

namespace RastreadorHabitos.Tests.Reportes;

// Regresión del Hallazgo 3 de la Asignación 3: con una zona horaria con horario de verano (la
// variable TZ), el reporte de solicitudes agrupaba con la diferencia con UTC de HOY, así que una
// solicitud del 1 de agosto a las 00:30 caía en «2026-07» aunque el filtro fuera del 1 de agosto;
// y si la medianoche del día del filtro no existe (Cuba adelanta el reloj a las 00:00), la
// petición respondía 500 (RF-REP-05, CR-06, RD-07).
public class Hallazgo3ZonaHorariaReportesTests
{
    // Reloj en un instante y una zona dados.
    private sealed class RelojEnZona(DateTimeOffset ahora, TimeZoneInfo zona) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => zona;
    }

    // Como America/New_York: UTC-5, y UTC-4 del segundo domingo de marzo al primero de noviembre.
    private static readonly TimeZoneInfo ConHorarioDeVerano = Zona("Prueba con horario de verano", horaInicio: 2);

    // Como America/Havana: igual, pero el reloj se adelanta a las 00:00, así que esa medianoche no existe.
    private static readonly TimeZoneInfo ConCambioAMedianoche = Zona("Prueba con cambio a medianoche", horaInicio: 0);

    private static TimeZoneInfo Zona(string nombre, int horaInicio) => TimeZoneInfo.CreateCustomTimeZone(
        nombre, TimeSpan.FromHours(-5), nombre, nombre, nombre,
        [TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2000, 1, 1), new DateTime(2100, 12, 31), TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, horaInicio, 0, 0), 3, 2, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 1, 0, 0), 11, 1, DayOfWeek.Sunday))]);

    private readonly string _sufijo = Guid.NewGuid().ToString("N");

    private GestionPermisosDbContext GestionPermisos() =>
        new(new DbContextOptionsBuilder<GestionPermisosDbContext>().UseInMemoryDatabase($"H3-GP-{_sufijo}").Options);

    // Hoy es el 18 de noviembre de 2026, ya fuera del horario de verano (UTC-5).
    private ReportesService Reportes(TimeZoneInfo zona)
    {
        var reloj = new RelojEnZona(new DateTimeOffset(2026, 11, 18, 15, 0, 0, TimeSpan.Zero), zona);
        var habitos = new HabitosDbContext(new DbContextOptionsBuilder<HabitosDbContext>().UseInMemoryDatabase($"H3-HAB-{_sufijo}").Options);
        return new ReportesService(
        [
            new FuenteSolicitudesPorEstado(new SolicitudesPermisoService(GestionPermisos(), new GestionPermisos.AplicadorPermisoFalso()), reloj),
            new FuenteCumplimientoSemanal(new MetricasHabitosService(habitos, reloj))
        ], reloj);
    }

    private async Task SolicitudAsync(DateTime creadaUtc)
    {
        await using var contexto = GestionPermisos();
        contexto.SolicitudesPermiso.Add(new SolicitudPermiso(Guid.NewGuid(), "LIMITE_HABITOS_AMPLIADO", creadaUtc));
        await contexto.SaveChangesAsync();
    }

    // [Hallazgo 3] El 1 de agosto a las 00:30 de verano (UTC-4) son las 04:30 UTC: cuenta en agosto,
    // el mismo mes que dice el filtro, aunque hoy la zona esté en UTC-5.
    [Fact]
    public async Task SolicitudesPorEstado_ConHorarioDeVerano_AgrupaEnElMesLocalDeCadaFecha()
    {
        // Preparar
        await SolicitudAsync(new DateTime(2026, 8, 1, 4, 30, 0));

        // Actuar
        var reporte = await Reportes(ConHorarioDeVerano)
            .GenerarAsync("solicitudes-por-estado", Guid.NewGuid(), esAdministrador: true, "2026-08-01", "2026-08-01");

        // Verificar
        var fila = Assert.Single(reporte.Filas);
        Assert.Equal("2026-08", fila.Grupo);
        Assert.Equal(1, fila.Valores["Total"]);
    }

    // [Hallazgo 3, RD-07] Un día cuya medianoche no existe empieza en su primer instante válido: el
    // filtro responde y cuenta lo de ese día, nunca falla.
    [Fact]
    public async Task SolicitudesPorEstado_ConUnaMedianocheQueNoExiste_NoFallaYFiltraElDia()
    {
        // Preparar: el 8 de marzo de 2026 el reloj salta de las 00:00 a la 01:00; a las 12:00
        // locales (UTC-4) son las 16:00 UTC
        await SolicitudAsync(new DateTime(2026, 3, 8, 16, 0, 0));
        var reportes = Reportes(ConCambioAMedianoche);

        // Actuar
        var desdeEseDia = await Record.ExceptionAsync(
            () => reportes.GenerarAsync("solicitudes-por-estado", Guid.NewGuid(), true, "2026-03-08", "2026-03-08"));
        var hastaElDiaAnterior = await Record.ExceptionAsync(
            () => reportes.GenerarAsync("solicitudes-por-estado", Guid.NewGuid(), true, null, "2026-03-07"));

        // Verificar
        Assert.Null(desdeEseDia);
        Assert.Null(hastaElDiaAnterior);
        var conElDia = await reportes.GenerarAsync("solicitudes-por-estado", Guid.NewGuid(), true, "2026-03-08", "2026-03-08");
        Assert.Equal(1, Assert.Single(conElDia.Filas).Valores["Total"]);
        var sinElDia = await reportes.GenerarAsync("solicitudes-por-estado", Guid.NewGuid(), true, null, "2026-03-07");
        Assert.Empty(sinElDia.Filas);
    }
}
