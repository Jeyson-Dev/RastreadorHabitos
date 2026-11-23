using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Metas.DTOs;
using RastreadorHabitos.Modulo.Metas.Reglas;

namespace RastreadorHabitos.Tests.Notificaciones;

// Regresión del Hallazgo 2 de la Asignación 3: dos consultas simultáneas de la misma meta leían
// las dos «Pendiente», las dos guardaban la transición a En progreso y las dos avisaban: el dueño
// recibía la notificación MetaCambioEstado repetida (CN-08, RF-NOT-03). En vivo, 8 consultas
// simultáneas dejaron entre 5 y 7 avisos por meta.
public class Hallazgo2ConsultasSimultaneasMetaTests
{
    private static RelojFijo Reloj() => new(new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.FromHours(-4)));

    // [Hallazgo 2] Una consulta que leyó la meta antes de que otra la pasara a En progreso no
    // vuelve a aplicar la transición ni a avisar.
    [Fact]
    public async Task ConsultarMeta_QueOtraConsultaYaCambio_NoDuplicaLaNotificacion()
    {
        // Preparar: meta Pendiente con el cumplimiento de hoy registrado
        var piezas = new PiezasNotificaciones(Reloj());
        var dueno = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var habito = await piezas.Habitos().CrearAsync(dueno, new CreacionHabitoDto { Nombre = "Leer", Dias = [1, 2, 3, 4, 5, 6, 7] });
        var meta = await piezas.Metas().CrearAsync(dueno, new CreacionMetaDto
        {
            Nombre = "Leer 5 veces", ObjetivoCumplimientos = 5, FechaLimite = new DateOnly(2026, 11, 26), HabitoIds = [habito.Id]
        });
        await piezas.Habitos().RegistrarCumplimientoAsync(dueno, habito.Id, new RegistroCumplimientoDto { Cumplido = true });

        // La segunda petición lee la meta (Pendiente) antes de que la primera la cambie.
        var contextoSegunda = piezas.MetasContexto();
        await contextoSegunda.Metas.Include(m => m.Habitos).SingleAsync(m => m.Id == meta.Id);
        await piezas.Metas().ObtenerAsync(dueno, meta.Id); // primera: Pendiente → En progreso, avisa

        // Actuar: la segunda termina con lo que había leído
        var segunda = await piezas.MetasCon(contextoSegunda).ObtenerAsync(dueno, meta.Id);

        // Verificar
        Assert.Equal("EnProgreso", segunda.Estado);
        var avisos = (await piezas.NotificacionesDeAsync(dueno)).Where(n => n.Tipo == AvisosMeta.TipoCambioEstado).ToList();
        Assert.Single(avisos);
        Assert.Single(await piezas.CorreosAsync(), c => c.Asunto.Contains("notificación"));
    }
}
