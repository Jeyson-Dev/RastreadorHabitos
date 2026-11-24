using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Api.Autorizacion;
using RastreadorHabitos.Api.Controllers;
using RastreadorHabitos.Core.Auditoria.Data;
using RastreadorHabitos.Core.Auditoria.Entities;
using RastreadorHabitos.Core.Auditoria.Services;
using RastreadorHabitos.Tests.Notificaciones;

namespace RastreadorHabitos.Tests.Auditoria;

// La consulta del historial (RF-AUD-07) y su inmutabilidad (RF-AUD-06), sin levantar la aplicación
// [RD-12]. Cada prueba siembra sus registros en una base en memoria de nombre único.
public class HistorialAuditoriaTests
{
    // Hoy es el 18 de noviembre de 2026, en UTC-4.
    private static readonly TimeSpan Desplazamiento = TimeSpan.FromHours(-4);
    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 18, 10, 0, 0, Desplazamiento));
    private readonly DbContextOptions<AuditoriaDbContext> _opciones =
        new DbContextOptionsBuilder<AuditoriaDbContext>().UseInMemoryDatabase($"A4-HIST-{Guid.NewGuid():N}").Options;

    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Luis = Guid.NewGuid();

    private static DateTime Utc(int dia, int hora, int minuto = 0) =>
        new DateTimeOffset(2026, 11, dia, hora, minuto, 0, Desplazamiento).UtcDateTime;

    private async Task SembrarAsync(params RegistroAuditoria[] registros)
    {
        await using var contexto = new AuditoriaDbContext(_opciones);
        contexto.RegistrosAuditoria.AddRange(registros);
        await contexto.SaveChangesAsync();
    }

    private HistorialAuditoria Historial() => new(new AuditoriaDbContext(_opciones), _reloj);

    private static RegistroAuditoria Registro(Guid quien, string entidad, DateTime fechaUtc) =>
        RegistroAuditoria.Crear(quien, "Accion", entidad, Guid.NewGuid().ToString(), new { a = 1 }, new { a = 2 }, fechaUtc);

    // [RF-AUD-07] Filtra por el usuario que actuó.
    [Fact]
    public async Task Consultar_PorUsuario_DevuelveSoloLoQueHizoEseUsuario()
    {
        // Preparar
        await SembrarAsync(Registro(Ana, "Usuario", Utc(17, 9)), Registro(Luis, "Usuario", Utc(17, 9)), Registro(Ana, "Documento", Utc(17, 10)));

        // Actuar
        var historial = await Historial().ConsultarAsync(Ana.ToString(), null, null, null);

        // Verificar
        Assert.Equal(2, historial.Total);
        Assert.All(historial.Registros, r => Assert.Equal(Ana, r.UsuarioId));
    }

    // [RF-AUD-07] Filtra por la entidad afectada.
    [Fact]
    public async Task Consultar_PorEntidad_DevuelveSoloEsaEntidad()
    {
        // Preparar
        await SembrarAsync(Registro(Ana, "Usuario", Utc(17, 9)), Registro(Luis, "Documento", Utc(17, 9)), Registro(Ana, "SolicitudPermiso", Utc(17, 10)));

        // Actuar
        var historial = await Historial().ConsultarAsync(null, "Documento", null, null);

        // Verificar
        var unico = Assert.Single(historial.Registros);
        Assert.Equal("Documento", unico.Entidad);
        Assert.Equal(2, unico.ValorNuevo.GetProperty("a").GetInt32());
    }

    // [RF-AUD-07] El rango incluye el último día completo (23:59 locales) y excluye el siguiente;
    // dos rangos distintos dan resultados distintos.
    [Fact]
    public async Task Consultar_PorRangoDeFechas_IncluyeElUltimoDiaCompleto()
    {
        // Preparar
        await SembrarAsync(Registro(Ana, "Usuario", Utc(16, 8)), Registro(Ana, "Usuario", Utc(17, 23, 59)), Registro(Ana, "Usuario", Utc(18, 0, 1)));

        // Actuar
        var hasta17 = await Historial().ConsultarAsync(null, null, "2026-11-16", "2026-11-17");
        var solo18 = await Historial().ConsultarAsync(null, null, "2026-11-18", "2026-11-18");

        // Verificar
        Assert.Equal(2, hasta17.Total);
        Assert.Contains(hasta17.Registros, r => r.FechaUtc == Utc(17, 23, 59));
        Assert.Equal(1, solo18.Total);
    }

    // [RF-AUD-07, RD-07] Un filtro inválido se rechaza de forma controlada.
    [Theory]
    [InlineData("no-es-un-guid", null, null, null)]
    [InlineData(null, "Usuario;DROP", null, null)]
    [InlineData(null, null, "30/11/2026", null)]
    [InlineData(null, null, "2026-02-30", null)]
    [InlineData(null, null, "2026-11-20", "2026-11-10")]
    [InlineData(null, null, null, "1999-12-31")]
    public async Task Consultar_ConUnFiltroInvalido_SeRechaza(string? usuarioId, string? entidad, string? desde, string? hasta)
    {
        // Preparar
        var historial = Historial();

        // Actuar
        var rechazo = await Record.ExceptionAsync(() => historial.ConsultarAsync(usuarioId, entidad, desde, hasta));

        // Verificar
        Assert.IsType<RechazoAuditoriaException>(rechazo);
    }

    // [RF-AUD-07, RD-06] La consulta del historial está reservada al Administrador: un Estándar
    // recibe rechazo, también si construye la petición a mano (la exigencia la aplica el servidor).
    [Fact]
    public void ConsultarHistorial_EstaReservadaAlAdministrador_UnEstandarNoLaConsulta()
    {
        // Preparar
        var acciones = typeof(AuditoriaController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Actuar
        var exigencias = acciones.Select(a => ExigenciasDeRol.De("Auditoria", a.Name)).ToList();

        // Verificar
        Assert.NotEmpty(exigencias);
        Assert.All(exigencias, e => Assert.Equal(Exigencia.Administrador, e));
    }

    // [RF-AUD-06] Ninguna operación expuesta modifica ni borra registros: el controller solo tiene
    // GET, el servicio solo consulta y la entidad no tiene propiedades con set público.
    [Fact]
    public void HistorialExpuesto_NoTieneNingunaOperacionDeEdicionNiDeBorrado()
    {
        // Preparar
        var acciones = typeof(AuditoriaController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Actuar
        var verbos = acciones.SelectMany(a => a.GetCustomAttributes<HttpMethodAttribute>()).SelectMany(v => v.HttpMethods).Distinct().ToList();
        var metodosDelServicio = typeof(IHistorialAuditoria).GetMethods().Select(m => m.Name).ToList();
        var setsPublicos = typeof(RegistroAuditoria).GetProperties().Where(p => p.SetMethod?.IsPublic == true).Select(p => p.Name).ToList();

        // Verificar
        Assert.Equal(["GET"], verbos);
        Assert.Equal(["ConsultarAsync"], metodosDelServicio);
        Assert.Empty(setsPublicos);
    }

    // [RF-AUD-06] Aunque un código intentara modificar o borrar un registro, el guardado se
    // rechaza y el registro sigue igual.
    [Fact]
    public async Task ModificarOBorrarUnRegistro_SeRechazaYElRegistroSigueIgual()
    {
        // Preparar
        await SembrarAsync(Registro(Ana, "Usuario", Utc(17, 9)));
        await using var paraBorrar = new AuditoriaDbContext(_opciones);
        paraBorrar.RegistrosAuditoria.Remove(await paraBorrar.RegistrosAuditoria.SingleAsync());
        await using var paraModificar = new AuditoriaDbContext(_opciones);
        paraModificar.Entry(await paraModificar.RegistrosAuditoria.SingleAsync()).Property(r => r.Accion).CurrentValue = "Alterada";

        // Actuar
        var alBorrar = await Record.ExceptionAsync(() => paraBorrar.SaveChangesAsync());
        var alModificar = await Record.ExceptionAsync(() => paraModificar.SaveChangesAsync());

        // Verificar
        Assert.IsType<InvalidOperationException>(alBorrar);
        Assert.IsType<InvalidOperationException>(alModificar);
        await using var verificacion = new AuditoriaDbContext(_opciones);
        Assert.Equal("Accion", (await verificacion.RegistrosAuditoria.SingleAsync()).Accion);
    }

    // [RD-03] Auditoría no depende de ninguna otra pieza ni del módulo de negocio.
    [Fact]
    public void CoreAuditoria_NoReferenciaANingunaOtraPiezaNiAlNegocio()
    {
        // Preparar
        var ensamblado = typeof(RegistroAuditoria).Assembly;

        // Actuar
        var propias = ensamblado.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("RastreadorHabitos")).ToList();

        // Verificar
        Assert.Empty(propias);
    }
}
