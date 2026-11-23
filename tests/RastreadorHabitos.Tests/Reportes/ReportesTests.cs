using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Api.Adaptadores;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Services;
using RastreadorHabitos.Core.Reportes.Reglas;
using RastreadorHabitos.Core.Reportes.Services;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.Entities;
using RastreadorHabitos.Modulo.Habitos.Services;
using RastreadorHabitos.Tests.Notificaciones;

namespace RastreadorHabitos.Tests.Reportes;

// Los dos reportes con sus fuentes reales (Gestión de permisos y Hábitos) sobre bases en memoria
// de nombre único y un reloj fijo, sin levantar la aplicación [RD-12].
public class ReportesTests
{
    // Hoy es el miércoles 18 de noviembre de 2026, 10:00 en UTC-4.
    private static readonly TimeSpan Desplazamiento = TimeSpan.FromHours(-4);
    private static readonly DateOnly Hoy = new(2026, 11, 18);

    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 18, 10, 0, 0, Desplazamiento));
    private readonly string _sufijo = Guid.NewGuid().ToString("N");

    private GestionPermisosDbContext GestionPermisos() =>
        new(new DbContextOptionsBuilder<GestionPermisosDbContext>().UseInMemoryDatabase($"GP-{_sufijo}").Options);

    private HabitosDbContext Habitos() =>
        new(new DbContextOptionsBuilder<HabitosDbContext>().UseInMemoryDatabase($"HAB-{_sufijo}").Options);

    private ReportesService Reportes() => new(
    [
        new FuenteSolicitudesPorEstado(new SolicitudesPermisoService(GestionPermisos(), new GestionPermisos.AplicadorPermisoFalso()), _reloj),
        new FuenteCumplimientoSemanal(new MetricasHabitosService(Habitos(), _reloj))
    ], _reloj);

    // Instante UTC de una hora local del día indicado.
    private static DateTime Utc(DateOnly dia, int hora, int minuto = 0) =>
        new DateTimeOffset(dia.ToDateTime(new TimeOnly(hora, minuto)), Desplazamiento).UtcDateTime;

    private async Task SolicitudAsync(Guid solicitante, DateTime creadaUtc, Action<SolicitudPermiso>? resolver = null)
    {
        await using var contexto = GestionPermisos();
        var solicitud = new SolicitudPermiso(solicitante, "LIMITE_HABITOS_AMPLIADO", creadaUtc);
        resolver?.Invoke(solicitud);
        contexto.SolicitudesPermiso.Add(solicitud);
        await contexto.SaveChangesAsync();
    }

    // Un hábito diario creado el día indicado, con sus registros (día, cumplido).
    private async Task HabitoAsync(Guid dueno, DateOnly creado, params (DateOnly dia, bool cumplido)[] registros)
    {
        await using var contexto = Habitos();
        var habito = new Habito
        {
            UsuarioId = dueno,
            Nombre = "Hábito de prueba",
            FechaCreacionUtc = Utc(creado, 8),
            Dias = Enumerable.Range(1, 7).Select(d => new HabitoDia { DiaSemana = d }).ToList(),
            Registros = registros.Select(r => new RegistroDiario { Fecha = r.dia, Cumplido = r.cumplido }).ToList()
        };
        contexto.Habitos.Add(habito);
        await contexto.SaveChangesAsync();
    }

    // [RF-REP-01, RF-REP-03] CR-01: una fila por mes con cada estado contado y el total.
    [Fact]
    public async Task SolicitudesPorEstado_AgrupaPorMesYCuentaCadaEstado()
    {
        // Preparar
        var usuario = Guid.NewGuid();
        var admin = Guid.NewGuid();
        await SolicitudAsync(usuario, Utc(Hoy, 9));
        await SolicitudAsync(usuario, Utc(Hoy, 9), s => { s.Aprobar(admin, DateTime.UtcNow); s.MarcarAplicada(); });
        await SolicitudAsync(usuario, Utc(Hoy, 9), s => s.Rechazar(admin, "motivo", DateTime.UtcNow));

        // Actuar
        var reporte = await Reportes().GenerarAsync("solicitudes-por-estado", admin, esAdministrador: true, null, null);

        // Verificar
        var fila = Assert.Single(reporte.Filas);
        Assert.Equal("2026-11", fila.Grupo);
        Assert.Equal(1, fila.Valores["Pendiente"]);
        Assert.Equal(0, fila.Valores["Aprobada"]);
        Assert.Equal(1, fila.Valores["Aplicada"]);
        Assert.Equal(1, fila.Valores["Rechazada"]);
        Assert.Equal(3, fila.Valores["Total"]);
    }

    // [RF-REP-01] El mes de cada solicitud es el local: el 31 de octubre a las 22:00 locales
    // (2 de la madrugada del 1 de noviembre en UTC) cuenta en octubre.
    [Fact]
    public async Task SolicitudesPorEstado_UsaElMesLocalDeLaCreacion()
    {
        // Preparar
        var usuario = Guid.NewGuid();
        await SolicitudAsync(usuario, Utc(new DateOnly(2026, 10, 31), 22));
        await SolicitudAsync(usuario, Utc(Hoy, 9));

        // Actuar
        var reporte = await Reportes().GenerarAsync("solicitudes-por-estado", usuario, esAdministrador: false, null, null);

        // Verificar
        Assert.Equal(["2026-10", "2026-11"], reporte.Filas.Select(f => f.Grupo));
    }

    // [RF-REP-04] CR-04: un Estándar solo cuenta sus solicitudes; el Administrador, las de todos.
    [Fact]
    public async Task SolicitudesPorEstado_UnEstandarSoloVeLasSuyasYElAdministradorLasDeTodos()
    {
        // Preparar
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        await SolicitudAsync(e1, Utc(Hoy, 9));
        await SolicitudAsync(e2, Utc(Hoy, 9));
        await SolicitudAsync(e2, Utc(Hoy, 9));

        // Actuar
        var deE1 = await Reportes().GenerarAsync("solicitudes-por-estado", e1, esAdministrador: false, null, null);
        var deAdmin = await Reportes().GenerarAsync("solicitudes-por-estado", Guid.NewGuid(), esAdministrador: true, null, null);

        // Verificar
        Assert.Equal(1, Assert.Single(deE1.Filas).Valores["Total"]);
        Assert.Equal(ReportesService.AlcancePropio, deE1.Alcance);
        Assert.Equal(3, Assert.Single(deAdmin.Filas).Valores["Total"]);
        Assert.Equal(ReportesService.AlcanceTodos, deAdmin.Alcance);
    }

    // [RF-REP-05] CR-05 y CR-06: el último día está incluido hasta las 23:59 locales; dos rangos
    // distintos dan resultados distintos.
    [Fact]
    public async Task SolicitudesPorEstado_IncluyeElUltimoDiaCompletoYDosRangosDanResultadosDistintos()
    {
        // Preparar
        var usuario = Guid.NewGuid();
        await SolicitudAsync(usuario, Utc(Hoy, 23, 59));
        var hoy = Hoy.ToString(RangoFechas.Formato);
        var ayer = Hoy.AddDays(-1).ToString(RangoFechas.Formato);

        // Actuar
        var conHoy = await Reportes().GenerarAsync("solicitudes-por-estado", usuario, false, hoy, hoy);
        var conAyer = await Reportes().GenerarAsync("solicitudes-por-estado", usuario, false, ayer, ayer);

        // Verificar
        Assert.Equal(1, Assert.Single(conHoy.Filas).Valores["Total"]);
        Assert.Empty(conAyer.Filas);
    }

    // [RF-REP-02, RF-REP-03] CR-02: cruza los días programados con los cumplidos, una fila por
    // semana que resume varios registros diarios.
    [Fact]
    public async Task CumplimientoSemanal_DaElPorcentajeDeDiasProgramadosCumplidosPorSemana()
    {
        // Preparar: creado el lunes 9; esa semana cumple 3 de 7 días, esta semana 2 de 3 (lun a mié)
        var usuario = Guid.NewGuid();
        var lunes9 = new DateOnly(2026, 11, 9);
        await HabitoAsync(usuario, lunes9,
            (lunes9, true), (lunes9.AddDays(1), true), (lunes9.AddDays(2), false), (lunes9.AddDays(3), true),
            (new DateOnly(2026, 11, 16), true), (new DateOnly(2026, 11, 17), true));

        // Actuar
        var reporte = await Reportes().GenerarAsync("cumplimiento-semanal", usuario, false, null, null);

        // Verificar
        Assert.Equal(2, reporte.Filas.Count); // dos filas que resumen seis registros
        var pasada = reporte.Filas[0];
        Assert.Equal("2026-11-09", pasada.Grupo);
        Assert.Equal(7, pasada.Valores["diasProgramados"]);
        Assert.Equal(3, pasada.Valores["diasCumplidos"]);
        Assert.Equal(42.9m, pasada.Valores["porcentajeCumplimiento"]);
        var actual = reporte.Filas[1];
        Assert.Equal("2026-11-16", actual.Grupo);
        Assert.Equal(3, actual.Valores["diasProgramados"]); // los días futuros no cuentan
        Assert.Equal(2, actual.Valores["diasCumplidos"]);
        Assert.Equal(66.7m, actual.Valores["porcentajeCumplimiento"]);
    }

    // [RF-REP-04] CR-04: un Estándar no obtiene los cumplimientos de otro usuario.
    [Fact]
    public async Task CumplimientoSemanal_UnEstandarSoloVeSusHabitos()
    {
        // Preparar
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        await HabitoAsync(e1, Hoy, (Hoy, true));
        await HabitoAsync(e2, Hoy, (Hoy, false));

        // Actuar
        var deE2 = await Reportes().GenerarAsync("cumplimiento-semanal", e2, false, null, null);
        var deAdmin = await Reportes().GenerarAsync("cumplimiento-semanal", Guid.NewGuid(), true, null, null);

        // Verificar
        var fila = Assert.Single(deE2.Filas);
        Assert.Equal(1, fila.Valores["diasProgramados"]);
        Assert.Equal(0, fila.Valores["diasCumplidos"]);
        Assert.Equal(2, Assert.Single(deAdmin.Filas).Valores["diasProgramados"]);
        Assert.Equal(1, Assert.Single(deAdmin.Filas).Valores["diasCumplidos"]);
    }

    // [RF-REP-05] CR-05 y CR-06: el rango filtra con los dos días incluidos.
    [Fact]
    public async Task CumplimientoSemanal_FiltraPorElRangoConLosDosDiasIncluidos()
    {
        // Preparar
        var usuario = Guid.NewGuid();
        var lunes = new DateOnly(2026, 11, 16);
        await HabitoAsync(usuario, lunes, (lunes, true), (lunes.AddDays(1), true), (Hoy, true));

        // Actuar: solo el martes 17 y el miércoles 18
        var reporte = await Reportes().GenerarAsync("cumplimiento-semanal", usuario, false, "2026-11-17", "2026-11-18");

        // Verificar
        var fila = Assert.Single(reporte.Filas);
        Assert.Equal(2, fila.Valores["diasProgramados"]);
        Assert.Equal(2, fila.Valores["diasCumplidos"]);
    }

    // [RF-REP-05] CR-07: un rango sin datos devuelve una lista vacía, no un error.
    [Fact]
    public async Task Reportes_ConUnRangoSinDatos_DevuelvenFilasVacias()
    {
        // Preparar
        var usuario = Guid.NewGuid();
        await SolicitudAsync(usuario, Utc(Hoy, 9));
        await HabitoAsync(usuario, Hoy, (Hoy, true));

        // Actuar
        var solicitudes = await Reportes().GenerarAsync("solicitudes-por-estado", usuario, false, "2020-01-01", "2020-01-31");
        var cumplimiento = await Reportes().GenerarAsync("cumplimiento-semanal", usuario, false, "2020-01-01", "2020-01-31");

        // Verificar
        Assert.Empty(solicitudes.Filas);
        Assert.Empty(cumplimiento.Filas);
        Assert.Equal(new DateOnly(2020, 1, 1), solicitudes.Desde);
    }

    // [RF-REP-05, RD-07] CR-08: un rango inválido se rechaza con un mensaje que dice qué corregir.
    [Theory]
    [InlineData("2026-11-20", "2026-11-10")]
    [InlineData("2026-02-30", null)]
    [InlineData("30/11/2026", null)]
    [InlineData("hoy", null)]
    [InlineData(null, "1999-12-31")]
    public async Task Reportes_ConUnRangoInvalido_SeRechazan(string? desde, string? hasta)
    {
        // Preparar
        var reportes = Reportes();

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoReportesException>(
            () => reportes.GenerarAsync("solicitudes-por-estado", Guid.NewGuid(), false, desde, hasta));

        // Verificar
        Assert.Equal(TipoRechazoReportes.DatosInvalidos, rechazo.Tipo);
        Assert.Contains("fecha", rechazo.Message);
    }

    // [RF-REP-05] El último día incluido: el primer instante excluido es la medianoche local del
    // día siguiente, en UTC.
    [Fact]
    public void RangoFechas_HastaIncluyeElDiaCompletoEnHoraLocal()
    {
        // Preparar
        var zona = TimeZoneInfo.CreateCustomTimeZone("Prueba", Desplazamiento, "Prueba", "Prueba");

        // Actuar
        var rango = RangoFechas.Leer("2026-11-18", "2026-11-18");

        // Verificar
        Assert.Equal(new DateTime(2026, 11, 18, 4, 0, 0), rango.DesdeUtc(zona));
        Assert.Equal(new DateTime(2026, 11, 19, 4, 0, 0), rango.HastaExclusivoUtc(zona));
    }
}
