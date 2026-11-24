using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.ControlAcceso.Services;

namespace RastreadorHabitos.Tests.ControlAcceso;

// [RF-CA-07] GET /api/sesion/usuario: además del rol, el Id y los permisos aplicados, para que el
// frontend muestre el límite de hábitos y no ofrezca acciones sobre uno mismo.
public class UsuarioAutenticadoTests
{
    private sealed class EmisorSinUso : IEmisorCredencialSesion
    {
        public string Emitir(SesionUsuario sesion, string nombreRol) => throw new NotSupportedException();
    }

    private static ControlAccesoDbContext NuevaBase()
    {
        var contexto = new ControlAccesoDbContext(new DbContextOptionsBuilder<ControlAccesoDbContext>()
            .UseInMemoryDatabase($"UsuarioAutenticado-{Guid.NewGuid():N}").Options);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    private static async Task<(Guid UsuarioId, Guid SesionId)> SembrarSesionAsync(ControlAccesoDbContext contexto,
                                                                                  params string[] permisos)
    {
        var usuario = new Usuario
        {
            Email = "ana@correo.com",
            NombreCompleto = "Ana Pérez",
            PasswordHash = "no-se-usa-en-esta-prueba",
            RolId = Rol.IdEstandar,
            CorreoConfirmado = true
        };
        var sesion = new SesionUsuario { UsuarioId = usuario.Id, FechaExpiracionUtc = DateTime.UtcNow.AddHours(8) };
        contexto.Usuarios.Add(usuario);
        contexto.SesionesUsuario.Add(sesion);
        contexto.PermisosUsuario.AddRange(permisos.Select(c => new PermisoUsuario { UsuarioId = usuario.Id, Codigo = c }));
        await contexto.SaveChangesAsync();
        return (usuario.Id, sesion.Id);
    }

    private static SesionService Servicio(ControlAccesoDbContext contexto) =>
        new(contexto, new OpcionesSesion { Clave = new string('x', OpcionesSesion.LongitudMinimaClave) }, new EmisorSinUso());

    // [RF-CA-07] Devuelve el Id del usuario y sus permisos aplicados.
    [Fact]
    public async Task ObtenerUsuarioAutenticado_IncluyeIdYPermisos()
    {
        // Preparar
        await using var contexto = NuevaBase();
        var (usuarioId, sesionId) = await SembrarSesionAsync(contexto, "LIMITE_HABITOS_AMPLIADO");

        // Actuar
        var usuario = await Servicio(contexto).ObtenerUsuarioAutenticadoAsync(sesionId);

        // Verificar
        Assert.Equal(usuarioId, usuario.Id);
        Assert.Equal(Rol.NombreEstandar, usuario.Rol);
        Assert.Equal(["LIMITE_HABITOS_AMPLIADO"], usuario.Permisos);
    }

    // [RF-CA-07] Sin permisos aplicados, la lista llega vacía (nunca null).
    [Fact]
    public async Task ObtenerUsuarioAutenticado_SinPermisos_DevuelveListaVacia()
    {
        // Preparar
        await using var contexto = NuevaBase();
        var (_, sesionId) = await SembrarSesionAsync(contexto);

        // Actuar
        var usuario = await Servicio(contexto).ObtenerUsuarioAutenticadoAsync(sesionId);

        // Verificar
        Assert.Empty(usuario.Permisos);
    }
}
