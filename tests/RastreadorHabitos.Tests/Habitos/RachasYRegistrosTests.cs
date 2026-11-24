using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Habitos.Entities;
using RastreadorHabitos.Modulo.Habitos.Reglas;
using RastreadorHabitos.Modulo.Habitos.Services;
using RastreadorHabitos.Tests.Notificaciones;

namespace RastreadorHabitos.Tests.Habitos;

// Rachas (regla 4 del diseño) y el historial de registros de solo lectura que consume el frontend.
// Cada prueba usa su propia base en memoria y un reloj fijo, sin levantar la aplicación [RD-12].
public class RachasYRegistrosTests
{
    private static readonly int[] TodaLaSemana = [1, 2, 3, 4, 5, 6, 7];
    private static readonly Guid Dueno = Guid.NewGuid();

    // Lunes 23 de noviembre de 2026, 10:00 en Santo Domingo (UTC-4).
    private static RelojFijo Reloj() => new(new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.FromHours(-4)));

    private sealed class SinPermisos : IPermisosUsuarioService
    {
        public Task<bool> TienePermisoAsync(Guid usuarioId, string codigo) => Task.FromResult(false);
        public Task AplicarPermisoAsync(Guid usuarioId, string codigo) => Task.CompletedTask;
    }

    // Reloj en un instante y una zona dados.
    private sealed class RelojEnZona(DateTimeOffset ahora, TimeZoneInfo zona) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => zona;
    }

    // Como America/New_York: UTC-5, y UTC-4 del segundo domingo de marzo al primero de noviembre.
    private static readonly TimeZoneInfo ConHorarioDeVerano = TimeZoneInfo.CreateCustomTimeZone(
        "Prueba con horario de verano", TimeSpan.FromHours(-5), "Prueba", "Prueba", "Prueba",
        [TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2000, 1, 1), new DateTime(2100, 12, 31), TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 1, 0, 0), 11, 1, DayOfWeek.Sunday))]);

    private static HabitosDbContext NuevaBase() =>
        new(new DbContextOptionsBuilder<HabitosDbContext>()
            .UseInMemoryDatabase($"Rachas-{Guid.NewGuid():N}").Options);

    private static async Task<Habito> SembrarHabitoAsync(HabitosDbContext contexto, Guid dueno, int[] dias,
                                                         DateTime creacionUtc, params (string Fecha, bool Cumplido)[] registros)
    {
        var habito = new Habito
        {
            UsuarioId = dueno,
            Nombre = "Leer",
            FechaCreacionUtc = creacionUtc,
            Dias = dias.Select(d => new HabitoDia { DiaSemana = d }).ToList()
        };
        contexto.Habitos.Add(habito);
        contexto.RegistrosDiarios.AddRange(registros.Select(r => new RegistroDiario
        {
            HabitoId = habito.Id, Fecha = DateOnly.Parse(r.Fecha), Cumplido = r.Cumplido
        }));
        await contexto.SaveChangesAsync();
        return habito;
    }

    private static (string, bool)[] Cumplidos(string desde, int dias) =>
        Enumerable.Range(0, dias).Select(i => (DateOnly.Parse(desde).AddDays(i).ToString("yyyy-MM-dd"), true)).ToArray();

    private static Dictionary<DateOnly, bool> Registros(params (string Fecha, bool Cumplido)[] registros) =>
        registros.ToDictionary(r => DateOnly.Parse(r.Fecha), r => r.Cumplido);

    // [Regla 4] Los días no programados no cuentan ni rompen la racha.
    [Fact]
    public void Calcular_CuentaSoloDiasProgramadosConsecutivosCumplidos()
    {
        // Preparar: lunes, miércoles y viernes, del lunes 2 al lunes 16 de noviembre, todos cumplidos.
        var registros = Registros(("2026-11-02", true), ("2026-11-04", true), ("2026-11-06", true),
                                  ("2026-11-09", true), ("2026-11-11", true), ("2026-11-13", true), ("2026-11-16", true));

        // Actuar
        var (actual, mejor) = Rachas.Calcular([1, 3, 5], registros, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 16));

        // Verificar
        Assert.Equal(7, actual);
        Assert.Equal(7, mejor);
    }

    // [Regla 4] Un día programado ya pasado sin registro rompe la racha; hoy sin registrar no.
    [Fact]
    public void Calcular_DiaPasadoSinRegistroRompe_YHoyPendienteNo()
    {
        // Preparar: cumple del 1 al 5, falta el 6, cumple del 7 al 9 y hoy (10) aún no registra.
        var registros = Registros([.. Cumplidos("2026-11-01", 5), .. Cumplidos("2026-11-07", 3)]);

        // Actuar
        var (actual, mejor) = Rachas.Calcular(TodaLaSemana, registros, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 10));

        // Verificar
        Assert.Equal(3, actual);
        Assert.Equal(5, mejor);
    }

    // [Regla 4] Registrar hoy como no cumplido rompe la racha actual, pero no la mejor.
    [Fact]
    public void Calcular_HoyNoCumplido_RompeLaRachaActual()
    {
        // Preparar
        var registros = Registros([.. Cumplidos("2026-11-01", 5), ("2026-11-06", false)]);

        // Actuar
        var (actual, mejor) = Rachas.Calcular(TodaLaSemana, registros, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 6));

        // Verificar
        Assert.Equal(0, actual);
        Assert.Equal(5, mejor);
    }

    // [Frontend] El listado trae las rachas y el estado de hoy de cada hábito.
    [Fact]
    public async Task Listar_DevuelveRachasYEstadoDeHoy()
    {
        // Preparar: creado el lunes 16, cumplido del 16 al 22; hoy (23) todavía sin registrar.
        await using var contexto = NuevaBase();
        await SembrarHabitoAsync(contexto, Dueno, TodaLaSemana, new DateTime(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc),
                                 Cumplidos("2026-11-16", 7));
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var habito = Assert.Single(await servicio.ListarAsync(Dueno));

        // Verificar
        Assert.Equal(7, habito.RachaActual);
        Assert.Equal(7, habito.MejorRacha);
        Assert.True(habito.ProgramadoHoy);
        Assert.Null(habito.CumplidoHoy);
    }

    // [Frontend] Al registrar hoy, el listado lo refleja y la racha crece.
    [Fact]
    public async Task Listar_TrasRegistrarHoy_MarcaCumplidoYSumaALaRacha()
    {
        // Preparar
        await using var contexto = NuevaBase();
        var sembrado = await SembrarHabitoAsync(contexto, Dueno, TodaLaSemana,
                                                new DateTime(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc), Cumplidos("2026-11-16", 7));
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        await servicio.RegistrarCumplimientoAsync(Dueno, sembrado.Id, new RegistroCumplimientoDto { Cumplido = true });
        var habito = Assert.Single(await servicio.ListarAsync(Dueno));

        // Verificar
        Assert.True(habito.CumplidoHoy);
        Assert.Equal(8, habito.RachaActual);
    }

    // [Frontend] Sin fechas, el historial cubre los últimos 30 días hasta hoy y deja fuera los anteriores.
    [Fact]
    public async Task ConsultarRegistros_SinFechas_DevuelveLosUltimos30Dias()
    {
        // Preparar
        await using var contexto = NuevaBase();
        var habito = await SembrarHabitoAsync(contexto, Dueno, TodaLaSemana, new DateTime(2026, 9, 30, 14, 0, 0, DateTimeKind.Utc),
                                              ("2026-10-01", true), ("2026-10-25", true), ("2026-11-22", false));
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var historial = await servicio.ConsultarRegistrosAsync(Dueno, habito.Id, null, null);

        // Verificar
        Assert.Equal(new DateOnly(2026, 10, 25), historial.Desde);
        Assert.Equal(new DateOnly(2026, 11, 23), historial.Hasta);
        Assert.Equal([new DateOnly(2026, 10, 25), new DateOnly(2026, 11, 22)], historial.Registros.Select(r => r.Fecha));
        Assert.Equal(0, historial.RachaActual);
        Assert.Equal(1, historial.MejorRacha);
    }

    // [RD-07] Fechas vacías o en blanco se toman como ausentes: el rango por defecto.
    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "  ")]
    public async Task ConsultarRegistros_FechasVaciasOEnBlanco_UsaElRangoPorDefecto(string desde, string hasta)
    {
        // Preparar
        await using var contexto = NuevaBase();
        var habito = await SembrarHabitoAsync(contexto, Dueno, TodaLaSemana, new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var historial = await servicio.ConsultarRegistrosAsync(Dueno, habito.Id, desde, hasta);

        // Verificar
        Assert.Equal(new DateOnly(2026, 10, 25), historial.Desde);
        Assert.Equal(new DateOnly(2026, 11, 23), historial.Hasta);
    }

    // [RD-07] Una fecha mal formada, un rango invertido o de más de 366 días se rechazan con un mensaje.
    [Theory]
    [InlineData("2026-13-01", null)]
    [InlineData("23/11/2026", null)]
    [InlineData(null, "ayer")]
    [InlineData("2026-11-20", "2026-11-10")]
    [InlineData("2025-01-01", "2026-11-23")]
    public async Task ConsultarRegistros_FechasInvalidas_SeRechazan(string? desde, string? hasta)
    {
        // Preparar
        await using var contexto = NuevaBase();
        var habito = await SembrarHabitoAsync(contexto, Dueno, TodaLaSemana, new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoHabitosException>(
            () => servicio.ConsultarRegistrosAsync(Dueno, habito.Id, desde, hasta));

        // Verificar
        Assert.Equal(TipoRechazoHabitos.DatosInvalidos, rechazo.Tipo);
        Assert.False(string.IsNullOrWhiteSpace(rechazo.Message));
    }

    // [RF-NEG-06] El historial de un hábito de otro usuario se trata como inexistente.
    [Fact]
    public async Task ConsultarRegistros_HabitoDeOtroUsuario_NoExiste()
    {
        // Preparar
        await using var contexto = NuevaBase();
        var ajeno = await SembrarHabitoAsync(contexto, Guid.NewGuid(), TodaLaSemana, new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoHabitosException>(
            () => servicio.ConsultarRegistrosAsync(Dueno, ajeno.Id, null, null));

        // Verificar
        Assert.Equal(TipoRechazoHabitos.NoEncontrado, rechazo.Tipo);
    }

    // [RD-11, Hallazgo 3] «Hoy» es el día local de la zona con horario de verano, no el día UTC.
    [Fact]
    public async Task Listar_ConZonaConHorarioDeVerano_UsaElDiaLocalParaHoy()
    {
        // Preparar: 23-nov 03:30 UTC es el domingo 22 a las 22:30 en la zona (UTC-5 en noviembre).
        await using var contexto = NuevaBase();
        await SembrarHabitoAsync(contexto, Dueno, [7], new DateTime(2026, 7, 6, 3, 30, 0, DateTimeKind.Utc),
                                 ("2026-11-22", true));
        var reloj = new RelojEnZona(new DateTimeOffset(2026, 11, 23, 3, 30, 0, TimeSpan.Zero), ConHorarioDeVerano);
        var servicio = new HabitosService(contexto, new SinPermisos(), reloj);

        // Actuar
        var habito = Assert.Single(await servicio.ListarAsync(Dueno));

        // Verificar
        Assert.True(habito.ProgramadoHoy);
        Assert.True(habito.CumplidoHoy);
        Assert.Equal(1, habito.RachaActual);
    }
}
