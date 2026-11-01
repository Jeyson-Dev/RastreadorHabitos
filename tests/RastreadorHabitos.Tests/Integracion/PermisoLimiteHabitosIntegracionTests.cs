using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Entities;
using RastreadorHabitos.Core.GestionPermisos.Services;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Habitos.Reglas;
using RastreadorHabitos.Modulo.Habitos.Services;

namespace RastreadorHabitos.Tests.Integracion;

// Integración de Gestión de permisos con el módulo de negocio (Práctica 3, sección 1.4): crear un
// hábito por encima del límite de 10 requiere el permiso LIMITE_HABITOS_AMPLIADO. El Estándar lo
// solicita, un Administrador lo aprueba, Gestión de permisos le pide a Control de acceso que lo
// aplique y Hábitos lo consulta a Control de acceso con tienePermiso. Todo con las piezas reales.
public class PermisoLimiteHabitosIntegracionTests
{
    private static CreacionHabitoDto NuevoHabito(int numero) =>
        new() { Nombre = $"Hábito {numero}", Dias = [1, 2, 3, 4, 5, 6, 7] };

    private static async Task CrearHabitosHastaElLimiteAsync(PiezasReales piezas, Guid usuarioId)
    {
        for (var numero = 1; numero <= LimiteHabitos.Normal; numero++)
        {
            await piezas.Habitos().CrearAsync(usuarioId, NuevoHabito(numero));
        }
    }

    // Las dos caras: el 11.º hábito se rechaza mientras la solicitud está Pendiente y se permite
    // después de que un Administrador la aprueba.
    [Fact]
    public async Task CrearHabitoSobreElLimite_SeRechazaConLaSolicitudPendienteYSePermiteTrasAprobarla()
    {
        // Preparar
        var piezas = new PiezasReales();
        var estandarId = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var administradorId = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        await CrearHabitosHastaElLimiteAsync(piezas, estandarId);
        var solicitud = await piezas.GestionPermisos().CrearAsync(estandarId,
            new CreacionSolicitudPermisoDto { Permiso = LimiteHabitos.PermisoAmpliado });

        // Actuar
        var rechazoAntesDeAprobar = await Record.ExceptionAsync(() =>
            piezas.Habitos().CrearAsync(estandarId, NuevoHabito(11)));
        var habitosAntesDeAprobar = await piezas.ContarHabitosAsync(estandarId);
        var aprobada = await piezas.GestionPermisos().AprobarAsync(solicitud.Id, administradorId);
        var creadoTrasAprobar = await piezas.Habitos().CrearAsync(estandarId, NuevoHabito(11));

        // Verificar
        var rechazo = Assert.IsType<RechazoHabitosException>(rechazoAntesDeAprobar);
        Assert.Equal(TipoRechazoHabitos.LimiteAlcanzado, rechazo.Tipo);
        Assert.Equal(LimiteHabitos.Normal, habitosAntesDeAprobar);
        Assert.Equal(nameof(EstadoSolicitud.Aplicada), aprobada.Estado);
        Assert.Equal("Hábito 11", creadoTrasAprobar.Nombre);
        Assert.Equal(LimiteHabitos.Normal + 1, await piezas.ContarHabitosAsync(estandarId));
    }

    // Si un Administrador rechaza la solicitud, el permiso no se concede y el 11.º hábito se
    // sigue rechazando.
    [Fact]
    public async Task CrearHabitoSobreElLimite_ConLaSolicitudRechazada_SigueRechazandose()
    {
        // Preparar
        var piezas = new PiezasReales();
        var estandarId = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var administradorId = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        await CrearHabitosHastaElLimiteAsync(piezas, estandarId);
        var solicitud = await piezas.GestionPermisos().CrearAsync(estandarId,
            new CreacionSolicitudPermisoDto { Permiso = LimiteHabitos.PermisoAmpliado });
        await piezas.GestionPermisos().RechazarAsync(solicitud.Id, administradorId,
            new RechazoSolicitudPermisoDto { Motivo = "Diez hábitos son suficientes." });

        // Actuar
        var rechazo = await Assert.ThrowsAsync<RechazoHabitosException>(() =>
            piezas.Habitos().CrearAsync(estandarId, NuevoHabito(11)));

        // Verificar
        Assert.Equal(TipoRechazoHabitos.LimiteAlcanzado, rechazo.Tipo);
        Assert.Equal(LimiteHabitos.Normal, await piezas.ContarHabitosAsync(estandarId));
    }

    // Aprobar el permiso no cambia el rol del solicitante: sigue siendo Estándar.
    [Fact]
    public async Task AprobarLimiteHabitosAmpliado_ConcedeElPermisoSinCambiarElRol()
    {
        // Preparar
        var piezas = new PiezasReales();
        var estandarId = await piezas.CrearUsuarioAsync(Rol.IdEstandar);
        var administradorId = await piezas.CrearUsuarioAsync(Rol.IdAdministrador);
        var solicitud = await piezas.GestionPermisos().CrearAsync(estandarId,
            new CreacionSolicitudPermisoDto { Permiso = LimiteHabitos.PermisoAmpliado });

        // Actuar
        await piezas.GestionPermisos().AprobarAsync(solicitud.Id, administradorId);

        // Verificar
        Assert.True(await piezas.TienePermisoAsync(estandarId, LimiteHabitos.PermisoAmpliado));
        Assert.Equal(Rol.IdEstandar, await piezas.RolDeAsync(estandarId));
    }

    // El módulo de negocio nunca habla con Gestión de permisos: consulta el permiso solo a
    // Control de acceso.
    [Fact]
    public void ModuloHabitos_NoReferenciaAGestionDePermisos()
    {
        // Preparar
        var moduloHabitos = typeof(HabitosService).Assembly;

        // Actuar
        var referencias = moduloHabitos.GetReferencedAssemblies().Select(a => a.Name).ToList();

        // Verificar
        Assert.DoesNotContain(typeof(SolicitudesPermisoService).Assembly.GetName().Name, referencias);
        Assert.Contains(typeof(Rol).Assembly.GetName().Name, referencias);
    }
}
