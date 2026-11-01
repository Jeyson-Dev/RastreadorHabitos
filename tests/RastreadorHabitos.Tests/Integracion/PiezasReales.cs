using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RastreadorHabitos.Api.Adaptadores;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.GestionPermisos.Services;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.Services;

namespace RastreadorHabitos.Tests.Integracion;

// Control de acceso, Gestión de permisos y el módulo de Hábitos reales, conectados como en la
// Api y sin dobles, cada uno con su propia base EF Core en memoria (un nombre único por
// instancia, así ninguna prueba depende de otra). No levanta la aplicación [RD-12].
public sealed class PiezasReales
{
    private readonly DbContextOptions<ControlAccesoDbContext> _controlAcceso =
        new DbContextOptionsBuilder<ControlAccesoDbContext>()
            .UseInMemoryDatabase($"ControlAcceso-{Guid.NewGuid()}")
            .Options;

    private readonly DbContextOptions<GestionPermisosDbContext> _gestionPermisos =
        new DbContextOptionsBuilder<GestionPermisosDbContext>()
            .UseInMemoryDatabase($"GestionPermisos-{Guid.NewGuid()}")
            // El proveedor en memoria no tiene transacciones; la de aprobar la da SQL Server.
            .ConfigureWarnings(avisos => avisos.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private readonly DbContextOptions<HabitosDbContext> _habitos =
        new DbContextOptionsBuilder<HabitosDbContext>()
            .UseInMemoryDatabase($"Habitos-{Guid.NewGuid()}")
            .Options;

    public PiezasReales()
    {
        // Crea la base con los dos roles del sistema.
        using var contexto = new ControlAccesoDbContext(_controlAcceso);
        contexto.Database.EnsureCreated();
    }

    // Un usuario activo con el rol indicado, como lo deja el registro con activación.
    public async Task<Guid> CrearUsuarioAsync(int rolId)
    {
        await using var contexto = new ControlAccesoDbContext(_controlAcceso);
        var usuario = new Usuario
        {
            Email = $"{Guid.NewGuid():N}@prueba.local",
            NombreCompleto = "Usuario de prueba",
            PasswordHash = "no-se-usa-en-esta-prueba",
            RolId = rolId,
            CorreoConfirmado = true
        };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }

    // Cada servicio recibe contextos nuevos, como en peticiones distintas a la Api.
    public SolicitudesPermisoService GestionPermisos()
    {
        var controlAcceso = new ControlAccesoDbContext(_controlAcceso);
        var aplicador = new AplicadorPermisoPorControlAcceso(
            new AdministracionUsuariosService(controlAcceso), new PermisosUsuarioService(controlAcceso));
        return new SolicitudesPermisoService(new GestionPermisosDbContext(_gestionPermisos), aplicador);
    }

    public HabitosService Habitos() =>
        new(new HabitosDbContext(_habitos),
            new PermisosUsuarioService(new ControlAccesoDbContext(_controlAcceso)),
            TimeProvider.System);

    public Task<bool> TienePermisoAsync(Guid usuarioId, string codigo) =>
        new PermisosUsuarioService(new ControlAccesoDbContext(_controlAcceso)).TienePermisoAsync(usuarioId, codigo);

    public async Task<int> RolDeAsync(Guid usuarioId)
    {
        await using var contexto = new ControlAccesoDbContext(_controlAcceso);
        return (await contexto.Usuarios.FindAsync(usuarioId))!.RolId;
    }

    public async Task<int> ContarHabitosAsync(Guid usuarioId)
    {
        await using var contexto = new HabitosDbContext(_habitos);
        return await contexto.Habitos.CountAsync(h => h.UsuarioId == usuarioId);
    }
}
