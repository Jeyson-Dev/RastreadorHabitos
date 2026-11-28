using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Metas.DTOs;
using RastreadorHabitos.Modulo.Metas.Entities;
using RastreadorHabitos.Modulo.Metas.Services;
using RastreadorHabitos.Tests.Notificaciones;

namespace RastreadorHabitos.Tests.Metas;

// Eliminar una meta y el vínculo de una meta con un hábito eliminado (sección 3 del diseño), con
// Hábitos y Metas reales sobre bases en memoria de nombre único, sin levantar la aplicación [RD-12].
public class EliminacionMetaTests
{
    // Lunes 16 de noviembre de 2026, 10:00 en UTC-4. Fijo: no depende del día en que corre la prueba.
    private static RelojFijo Reloj() => new(new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.FromHours(-4)));

    private static readonly DateOnly Hoy = new(2026, 11, 16);

    private static async Task<Guid> HabitoAsync(PiezasNotificaciones piezas, Guid dueno, string nombre) =>
        (await piezas.Habitos().CrearAsync(dueno, new CreacionHabitoDto { Nombre = nombre, Dias = [1, 2, 3, 4, 5, 6, 7] })).Id;

    private static async Task<Guid> MetaAsync(PiezasNotificaciones piezas, Guid dueno, int objetivo, params Guid[] habitos) =>
        (await piezas.Metas().CrearAsync(dueno, new CreacionMetaDto
        {
            Nombre = "Constancia",
            ObjetivoCumplimientos = objetivo,
            FechaLimite = Hoy.AddDays(10),
            HabitoIds = habitos.ToList()
        })).Id;

    // [RF-NEG-02] Eliminar una meta propia la borra con sus vínculos: deja de listarse y sus
    // hábitos siguen ahí.
    [Fact]
    public async Task Eliminar_MetaPropia_DejaDeListarseYBorraSusVinculos()
    {
        // Preparar
        var piezas = new PiezasNotificaciones(Reloj());
        var dueno = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var habito = await HabitoAsync(piezas, dueno, "Leer");
        var meta = await MetaAsync(piezas, dueno, 3, habito);

        // Actuar
        await piezas.Metas().EliminarAsync(dueno, meta);

        // Verificar
        await using var contexto = piezas.MetasContexto();
        Assert.Empty(await piezas.Metas().ListarAsync(dueno));
        Assert.Empty(await contexto.Set<MetaHabito>().ToListAsync());
        Assert.Single(await piezas.Habitos().ListarAsync(dueno));
    }

    // [RF-NEG-02, RF-NEG-05] Eliminar no es una transición: una meta Completada (terminal) también
    // se puede eliminar.
    [Fact]
    public async Task Eliminar_MetaCompletada_LaBorra()
    {
        // Preparar
        var piezas = new PiezasNotificaciones(Reloj());
        var dueno = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var habito = await HabitoAsync(piezas, dueno, "Leer");
        var meta = await MetaAsync(piezas, dueno, 1, habito);
        await piezas.Habitos().RegistrarCumplimientoAsync(dueno, habito, new RegistroCumplimientoDto { Cumplido = true });
        Assert.Equal("Completada", (await piezas.Metas().CompletarAsync(dueno, meta)).Estado);

        // Actuar
        await piezas.Metas().EliminarAsync(dueno, meta);

        // Verificar
        var rechazo = await Assert.ThrowsAsync<RechazoMetasException>(() => piezas.Metas().ObtenerAsync(dueno, meta));
        Assert.Equal(TipoRechazoMetas.NoEncontrado, rechazo.Tipo);
    }

    // [RD-06] La meta de otro usuario se trata como inexistente y sigue existiendo.
    [Fact]
    public async Task Eliminar_MetaDeOtroUsuario_SeRechazaYSigueExistiendo()
    {
        // Preparar
        var piezas = new PiezasNotificaciones(Reloj());
        var dueno = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var otro = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var meta = await MetaAsync(piezas, dueno, 3, await HabitoAsync(piezas, dueno, "Leer"));

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoMetasException>(() => piezas.Metas().EliminarAsync(otro, meta));

        // Verificar
        Assert.Equal(TipoRechazoMetas.NoEncontrado, rechazo.Tipo);
        Assert.Single(await piezas.Metas().ListarAsync(dueno));
    }

    // [Sección 3 del diseño] Si el dueño elimina un hábito, la meta descarta ese vínculo y conserva
    // los demás; su avance deja de contar los registros del hábito eliminado.
    [Fact]
    public async Task EliminarHabito_LaMetaDescartaElVinculoYConservaLosDemas()
    {
        // Preparar
        var piezas = new PiezasNotificaciones(Reloj());
        var dueno = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var leer = await HabitoAsync(piezas, dueno, "Leer");
        var correr = await HabitoAsync(piezas, dueno, "Correr");
        var meta = await MetaAsync(piezas, dueno, 5, leer, correr);
        await piezas.Habitos().RegistrarCumplimientoAsync(dueno, leer, new RegistroCumplimientoDto { Cumplido = true });
        await piezas.Habitos().RegistrarCumplimientoAsync(dueno, correr, new RegistroCumplimientoDto { Cumplido = true });

        // Actuar
        await piezas.Habitos().EliminarAsync(dueno, leer);
        var consultada = await piezas.Metas().ObtenerAsync(dueno, meta);

        // Verificar
        await using var contexto = piezas.MetasContexto();
        Assert.Equal([correr], consultada.HabitoIds);
        Assert.Equal(1, consultada.Avance);
        Assert.Equal(correr, (await contexto.Set<MetaHabito>().SingleAsync()).HabitoId);
    }
}
