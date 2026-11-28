using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Habitos.Entities;
using RastreadorHabitos.Modulo.Habitos.Reglas;
using RastreadorHabitos.Modulo.Habitos.Services;
using RastreadorHabitos.Tests.Notificaciones;

namespace RastreadorHabitos.Tests.Habitos;

// Editar, archivar, restaurar y eliminar un hábito (reglas 2 y 3 del diseño de componentes). Cada
// prueba usa su propia base en memoria y un reloj fijo, sin levantar la aplicación [RD-12].
public class EdicionArchivoEliminacionTests
{
    private static readonly int[] TodaLaSemana = [1, 2, 3, 4, 5, 6, 7];

    // Lunes 23 de noviembre de 2026, 10:00 en Santo Domingo (UTC-4).
    private static RelojFijo Reloj() => new(new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.FromHours(-4)));

    private static readonly DateTime LunesAnteriorUtc = new(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc);

    private sealed class SinPermisos : IPermisosUsuarioService
    {
        public Task<bool> TienePermisoAsync(Guid usuarioId, string codigo) => Task.FromResult(false);
        public Task AplicarPermisoAsync(Guid usuarioId, string codigo) => Task.CompletedTask;
    }

    // Pronóstico fijo: el plan de hoy no depende del servicio de clima real.
    private sealed class PronosticoFijo : IProveedorPronostico
    {
        public Task<ResultadoPronostico> ObtenerDeHoyAsync(string ciudad, CancellationToken cancelacion = default) =>
            Task.FromResult(ResultadoPronostico.Con(new PronosticoDia(ciudad, "País", new DateOnly(2026, 11, 23),
                                                                      "Soleado", 30, 22, 0)));
    }

    // Un nombre de base por prueba: los contextos que lo comparten ven los mismos datos.
    private static Func<HabitosDbContext> NuevaBase()
    {
        var nombre = $"Edicion-{Guid.NewGuid():N}";
        return () => new HabitosDbContext(new DbContextOptionsBuilder<HabitosDbContext>().UseInMemoryDatabase(nombre).Options);
    }

    private static async Task<Habito> SembrarAsync(HabitosDbContext contexto, Guid dueno, string nombre = "Leer",
                                                   DateTime? archivadoUtc = null, params string[] cumplidos)
    {
        var habito = new Habito
        {
            UsuarioId = dueno,
            Nombre = nombre,
            FechaCreacionUtc = LunesAnteriorUtc,
            FechaArchivadoUtc = archivadoUtc,
            Dias = TodaLaSemana.Select(d => new HabitoDia { DiaSemana = d }).ToList()
        };
        contexto.Habitos.Add(habito);
        contexto.RegistrosDiarios.AddRange(cumplidos.Select(fecha => new RegistroDiario
        {
            HabitoId = habito.Id, Fecha = DateOnly.Parse(fecha), Cumplido = true
        }));
        await contexto.SaveChangesAsync();
        return habito;
    }

    private static string[] Dias(string desde, int cantidad) =>
        Enumerable.Range(0, cantidad).Select(i => DateOnly.Parse(desde).AddDays(i).ToString("yyyy-MM-dd")).ToArray();

    // [Regla 3, RF-NEG-02] Editar reemplaza el nombre, la descripción y los días del hábito.
    [Fact]
    public async Task Editar_CambiaNombreDescripcionYDias()
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        var habito = await SembrarAsync(contexto, dueno);
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var editado = await servicio.EditarAsync(dueno, habito.Id,
            new EdicionHabitoDto { Nombre = "  Leer 30 minutos  ", Descripcion = "Antes de dormir", Dias = [1, 3, 5, 3] });

        // Verificar
        await using var otraLectura = base_();
        var guardado = await otraLectura.Habitos.Include(h => h.Dias).SingleAsync(h => h.Id == habito.Id);
        Assert.Equal("Leer 30 minutos", editado.Nombre);
        Assert.Equal("Leer 30 minutos", guardado.Nombre);
        Assert.Equal("Antes de dormir", guardado.Descripcion);
        Assert.Equal([1, 3, 5], guardado.Dias.Select(d => d.DiaSemana).Order());
    }

    // [RD-07, Regla 1 de la Asignación 3] Un nombre ausente, vacío o de solo espacios se rechaza y
    // el hábito no cambia.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Editar_ConNombreVacioOSoloEspacios_SeRechazaYNoCambia(string? nombre)
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        var habito = await SembrarAsync(contexto, dueno);
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoHabitosException>(() =>
            servicio.EditarAsync(dueno, habito.Id, new EdicionHabitoDto { Nombre = nombre, Dias = [1] }));

        // Verificar
        await using var otraLectura = base_();
        var guardado = await otraLectura.Habitos.Include(h => h.Dias).SingleAsync(h => h.Id == habito.Id);
        Assert.Equal(TipoRechazoHabitos.DatosInvalidos, rechazo.Tipo);
        Assert.Equal("Leer", guardado.Nombre);
        Assert.Equal(7, guardado.Dias.Count);
    }

    // [RD-07] Sin días, o con un día fuera de 1 a 7, la edición se rechaza.
    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 0 })]
    [InlineData(new[] { 8 })]
    public async Task Editar_SinDiasODiaFueraDeRango_SeRechaza(int[] dias)
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        var habito = await SembrarAsync(contexto, dueno);
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoHabitosException>(() =>
            servicio.EditarAsync(dueno, habito.Id, new EdicionHabitoDto { Nombre = "Leer", Dias = dias.ToList() }));

        // Verificar
        Assert.Equal(TipoRechazoHabitos.DatosInvalidos, rechazo.Tipo);
    }

    // [RD-06] El hábito de otro usuario se trata como inexistente y no cambia.
    [Fact]
    public async Task Editar_HabitoDeOtroUsuario_SeRechazaComoInexistente()
    {
        // Preparar
        var base_ = NuevaBase();
        await using var contexto = base_();
        var habito = await SembrarAsync(contexto, Guid.NewGuid());
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoHabitosException>(() =>
            servicio.EditarAsync(Guid.NewGuid(), habito.Id, new EdicionHabitoDto { Nombre = "Ajeno", Dias = [1] }));

        // Verificar
        await using var otraLectura = base_();
        Assert.Equal(TipoRechazoHabitos.NoEncontrado, rechazo.Tipo);
        Assert.Equal("Leer", (await otraLectura.Habitos.SingleAsync()).Nombre);
    }

    // [Regla 2, Regla 3] Un hábito archivado no cuenta para el límite: con 10 activos, archivar uno
    // deja crear otro.
    [Fact]
    public async Task Archivar_LiberaUnLugarDelLimite()
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        var habitos = new List<Habito>();
        for (var i = 0; i < LimiteHabitos.Normal; i++)
        {
            habitos.Add(await SembrarAsync(contexto, dueno, $"Hábito {i}"));
        }
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var archivado = await servicio.ArchivarAsync(dueno, habitos[0].Id);
        var creado = await servicio.CrearAsync(dueno, new CreacionHabitoDto { Nombre = "Uno más", Dias = [1] });

        // Verificar
        Assert.True(archivado.Archivado);
        Assert.NotNull(archivado.FechaArchivadoUtc);
        Assert.False(creado.Archivado);
        Assert.Equal(LimiteHabitos.Normal + 1, await contexto.Habitos.CountAsync(h => h.UsuarioId == dueno));
    }

    // [Regla 2] Restaurar ocupa un lugar del límite: con 10 activos se rechaza y sigue archivado.
    [Fact]
    public async Task Restaurar_ConElLimiteLleno_SeRechazaYSigueArchivado()
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        var archivado = await SembrarAsync(contexto, dueno, "Archivado", archivadoUtc: LunesAnteriorUtc);
        for (var i = 0; i < LimiteHabitos.Normal; i++)
        {
            await SembrarAsync(contexto, dueno, $"Hábito {i}");
        }
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoHabitosException>(() => servicio.RestaurarAsync(dueno, archivado.Id));

        // Verificar
        await using var otraLectura = base_();
        Assert.Equal(TipoRechazoHabitos.LimiteAlcanzado, rechazo.Tipo);
        Assert.NotNull((await otraLectura.Habitos.SingleAsync(h => h.Id == archivado.Id)).FechaArchivadoUtc);
    }

    // [Regla 3] Restaurar un hábito archivado lo vuelve a activar.
    [Fact]
    public async Task Restaurar_HabitoArchivado_LoVuelveAActivar()
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        var archivado = await SembrarAsync(contexto, dueno, archivadoUtc: LunesAnteriorUtc);
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var restaurado = await servicio.RestaurarAsync(dueno, archivado.Id);

        // Verificar
        Assert.False(restaurado.Archivado);
        Assert.Null(restaurado.FechaArchivadoUtc);
    }

    // [Regla 3] Archivar un hábito ya archivado, o restaurar uno activo, se rechaza.
    [Fact]
    public async Task ArchivarDosVecesYRestaurarUnoActivo_SeRechazan()
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        var activo = await SembrarAsync(contexto, dueno, "Activo");
        var archivado = await SembrarAsync(contexto, dueno, "Archivado", archivadoUtc: LunesAnteriorUtc);
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var otraVez = await Assert.ThrowsAsync<RechazoHabitosException>(() => servicio.ArchivarAsync(dueno, archivado.Id));
        var yaActivo = await Assert.ThrowsAsync<RechazoHabitosException>(() => servicio.RestaurarAsync(dueno, activo.Id));

        // Verificar
        Assert.Equal(TipoRechazoHabitos.ReglaIncumplida, otraVez.Tipo);
        Assert.Equal(TipoRechazoHabitos.ReglaIncumplida, yaActivo.Tipo);
    }

    // [Regla 3] Un hábito archivado no se registra y no se crea ningún registro.
    [Fact]
    public async Task Registrar_HabitoArchivado_SeRechazaSinCrearRegistro()
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        var archivado = await SembrarAsync(contexto, dueno, archivadoUtc: LunesAnteriorUtc);
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoHabitosException>(() =>
            servicio.RegistrarCumplimientoAsync(dueno, archivado.Id, new RegistroCumplimientoDto { Cumplido = true }));

        // Verificar
        Assert.Equal(TipoRechazoHabitos.ReglaIncumplida, rechazo.Tipo);
        Assert.Empty(await contexto.RegistrosDiarios.ToListAsync());
    }

    // [Regla 3, Regla 4] Archivar conserva la racha: pasan días sin registrar y no se rompe.
    [Fact]
    public async Task Archivar_ConservaLaRachaAunquePasenDias()
    {
        // Preparar: cumplido del lunes 16 al lunes 23 (hoy): racha de 8.
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        var reloj = Reloj();
        await using var contexto = base_();
        var habito = await SembrarAsync(contexto, dueno, cumplidos: Dias("2026-11-16", 8));
        var servicio = new HabitosService(contexto, new SinPermisos(), reloj);
        await servicio.ArchivarAsync(dueno, habito.Id);

        // Actuar: cinco días después, sin registrar nada.
        reloj.AdelantarDias(5);
        var listado = Assert.Single(await servicio.ListarAsync(dueno));
        var historial = await servicio.ConsultarRegistrosAsync(dueno, habito.Id, null, null);

        // Verificar
        Assert.True(listado.Archivado);
        Assert.Equal(8, listado.RachaActual);
        Assert.Equal(8, historial.RachaActual);
        Assert.Equal(8, historial.MejorRacha);
    }

    // [Regla 3, plan de hoy de la Práctica 4] Un hábito archivado no aparece en el plan de hoy.
    [Fact]
    public async Task PlanDeHoy_NoIncluyeHabitosArchivados()
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        await SembrarAsync(contexto, dueno, "Activo");
        await SembrarAsync(contexto, dueno, "Archivado", archivadoUtc: LunesAnteriorUtc);
        var plan = new PlanDeHoyService(contexto, new PronosticoFijo(), Reloj());

        // Actuar
        var deHoy = await plan.ObtenerAsync(dueno, "Santo Domingo");

        // Verificar
        Assert.Equal("Activo", Assert.Single(deHoy.Habitos).Nombre);
    }

    // [RF-REP-02, Regla 3] El reporte semanal no cuenta como programados los días posteriores al
    // archivo: no bajan el porcentaje de un hábito que el usuario dejó de seguir.
    [Fact]
    public async Task CumplimientoSemanal_NoCuentaLosDiasDespuesDeArchivar()
    {
        // Preparar: cumplido del lunes 16 al viernes 20 y archivado ese viernes a las 18:00 locales.
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        await SembrarAsync(contexto, dueno, archivadoUtc: new DateTime(2026, 11, 20, 22, 0, 0, DateTimeKind.Utc),
                           cumplidos: Dias("2026-11-16", 5));
        var metricas = new MetricasHabitosService(contexto, Reloj());

        // Actuar
        var semanas = await metricas.CumplimientoSemanalAsync(dueno, new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 23));

        // Verificar: solo la semana del 16, con 5 días programados (lunes a viernes) y 5 cumplidos.
        var semana = Assert.Single(semanas);
        Assert.Equal(new DateOnly(2026, 11, 16), semana.Semana);
        Assert.Equal(5, semana.DiasProgramados);
        Assert.Equal(5, semana.DiasCumplidos);
        Assert.Equal(100m, semana.PorcentajeCumplimiento);
    }

    // [Regla 3] Eliminar borra el hábito, sus días y sus registros diarios, y no toca los demás.
    [Fact]
    public async Task Eliminar_BorraElHabitoYSusRegistros()
    {
        // Preparar
        var base_ = NuevaBase();
        var dueno = Guid.NewGuid();
        await using var contexto = base_();
        var habito = await SembrarAsync(contexto, dueno, cumplidos: Dias("2026-11-16", 3));
        var otro = await SembrarAsync(contexto, dueno, "Otro", cumplidos: Dias("2026-11-16", 2));
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        await servicio.EliminarAsync(dueno, habito.Id);

        // Verificar
        await using var otraLectura = base_();
        Assert.Equal(otro.Id, (await otraLectura.Habitos.SingleAsync()).Id);
        Assert.All(await otraLectura.RegistrosDiarios.ToListAsync(), r => Assert.Equal(otro.Id, r.HabitoId));
        Assert.Equal(2, await otraLectura.RegistrosDiarios.CountAsync());
    }

    // [RD-06] Eliminar el hábito de otro usuario se rechaza como inexistente y el hábito sigue ahí.
    [Fact]
    public async Task Eliminar_HabitoDeOtroUsuario_SeRechazaYSigueExistiendo()
    {
        // Preparar
        var base_ = NuevaBase();
        await using var contexto = base_();
        var habito = await SembrarAsync(contexto, Guid.NewGuid(), cumplidos: Dias("2026-11-16", 1));
        var servicio = new HabitosService(contexto, new SinPermisos(), Reloj());

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoHabitosException>(() => servicio.EliminarAsync(Guid.NewGuid(), habito.Id));

        // Verificar
        await using var otraLectura = base_();
        Assert.Equal(TipoRechazoHabitos.NoEncontrado, rechazo.Tipo);
        Assert.Equal(1, await otraLectura.Habitos.CountAsync());
        Assert.Equal(1, await otraLectura.RegistrosDiarios.CountAsync());
    }
}
