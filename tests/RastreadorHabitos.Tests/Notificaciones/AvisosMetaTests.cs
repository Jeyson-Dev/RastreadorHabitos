using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Metas.DTOs;
using RastreadorHabitos.Modulo.Metas.Reglas;
using RastreadorHabitos.Modulo.Metas.Services;

namespace RastreadorHabitos.Tests.Notificaciones;

// Evento propio del módulo de negocio: un cambio de estado de una meta notifica a su dueño.
public class AvisosMetaTests
{
    // Lunes 16 de noviembre de 2026, 10:00 en UTC-4. Fijo: no depende del día en que corre la prueba.
    private static RelojFijo Reloj() => new(new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.FromHours(-4)));

    private static readonly DateOnly Hoy = new(2026, 11, 16);

    private static async Task<(Guid dueno, Guid habito, Guid meta)> MetaPendienteAsync(PiezasNotificaciones piezas)
    {
        var dueno = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var habito = await piezas.Habitos().CrearAsync(dueno,
            new CreacionHabitoDto { Nombre = "Leer 20 minutos", Dias = [1, 2, 3, 4, 5, 6, 7] });
        var meta = await piezas.Metas().CrearAsync(dueno, new CreacionMetaDto
        {
            Nombre = "Leer 2 veces",
            ObjetivoCumplimientos = 2,
            FechaLimite = Hoy.AddDays(10),
            HabitoIds = [habito.Id]
        });
        return (dueno, habito.Id, meta.Id);
    }

    // [RF-NOT-03] CN-08: el primer cumplimiento pasa la meta a En progreso y su dueño es notificado.
    [Fact]
    public async Task ConsultarMeta_QueCambiaAEnProgreso_NotificaASuDuenoElEstadoAnteriorYElNuevo()
    {
        // Preparar
        var piezas = new PiezasNotificaciones(Reloj());
        var otro = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var (dueno, habito, meta) = await MetaPendienteAsync(piezas);
        await piezas.Habitos().RegistrarCumplimientoAsync(dueno, habito, new RegistroCumplimientoDto { Cumplido = true });

        // Actuar
        var consultada = await piezas.Metas().ObtenerAsync(dueno, meta);

        // Verificar
        Assert.Equal("EnProgreso", consultada.Estado);
        var aviso = Assert.Single(await piezas.NotificacionesDeAsync(dueno));
        Assert.Equal(AvisosMeta.TipoCambioEstado, aviso.Tipo);
        Assert.Contains("Leer 2 veces", aviso.Mensaje);
        Assert.Contains("Pendiente", aviso.Mensaje);
        Assert.Contains("En progreso", aviso.Mensaje);
        Assert.Empty(await piezas.NotificacionesDeAsync(otro));
    }

    // [RF-NOT-03] CN-08: crear una meta o consultarla sin que cambie de estado no notifica.
    [Fact]
    public async Task ConsultarMeta_SinCambioDeEstado_NoNotifica()
    {
        // Preparar
        var piezas = new PiezasNotificaciones(Reloj());
        var (dueno, _, meta) = await MetaPendienteAsync(piezas);

        // Actuar
        await piezas.Metas().ObtenerAsync(dueno, meta);
        await piezas.Metas().ListarAsync(dueno);

        // Verificar
        Assert.Empty(await piezas.NotificacionesDeAsync(dueno));
    }

    // [RF-NOT-03] CN-09: vencer sin alcanzar el objetivo deja la meta Fallida y lo notifica.
    [Fact]
    public async Task ConsultarMeta_QueVencio_NotificaQueQuedoFallida()
    {
        // Preparar
        var reloj = Reloj();
        var piezas = new PiezasNotificaciones(reloj);
        var (dueno, _, meta) = await MetaPendienteAsync(piezas);
        reloj.AdelantarDias(11); // la fecha límite era hoy + 10

        // Actuar
        var consultada = await piezas.Metas().ObtenerAsync(dueno, meta);

        // Verificar
        Assert.Equal("Fallida", consultada.Estado);
        var aviso = Assert.Single(await piezas.NotificacionesDeAsync(dueno));
        Assert.Contains("Fallida", aviso.Mensaje);
    }

    // [RF-NOT-03] CN-09: completar la meta (por su dueño) también notifica el cambio de estado.
    [Fact]
    public async Task CompletarMeta_NotificaASuDuenoQueQuedoCompletada()
    {
        // Preparar: objetivo de 2 con un hábito diario, cumplido hoy y mañana
        var reloj = Reloj();
        var piezas = new PiezasNotificaciones(reloj);
        var (dueno, habito, meta) = await MetaPendienteAsync(piezas);
        await piezas.Habitos().RegistrarCumplimientoAsync(dueno, habito, new RegistroCumplimientoDto { Cumplido = true });
        await piezas.Metas().ObtenerAsync(dueno, meta); // Pendiente → En progreso (primer aviso)
        reloj.AdelantarDias(1);
        await piezas.Habitos().RegistrarCumplimientoAsync(dueno, habito, new RegistroCumplimientoDto { Cumplido = true });

        // Actuar
        var completada = await piezas.Metas().CompletarAsync(dueno, meta);

        // Verificar
        Assert.Equal("Completada", completada.Estado);
        var avisos = await piezas.NotificacionesDeAsync(dueno);
        Assert.Equal(2, avisos.Count);
        Assert.Contains(avisos, n => n.Mensaje.Contains("estaba en En progreso y ahora está en Completada"));
    }

    // [RF-NOT-03] CN-09: una transición rechazada (Pendiente → Completada, la prohibida) no notifica.
    [Fact]
    public async Task CompletarMeta_Pendiente_SeRechazaYNoNotifica()
    {
        // Preparar
        var piezas = new PiezasNotificaciones(Reloj());
        var (dueno, _, meta) = await MetaPendienteAsync(piezas);

        // Actuar
        await Assert.ThrowsAsync<RechazoMetasException>(() => piezas.Metas().CompletarAsync(dueno, meta));

        // Verificar
        Assert.Empty(await piezas.NotificacionesDeAsync(dueno));
    }
}
