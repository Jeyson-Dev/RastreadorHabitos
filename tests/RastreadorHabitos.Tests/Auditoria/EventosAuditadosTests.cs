using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RastreadorHabitos.Api.Adaptadores;
using RastreadorHabitos.Core.Auditoria.Entities;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.DTOs;
using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.Documentos.Data;
using RastreadorHabitos.Core.Documentos.DTOs;
using RastreadorHabitos.Core.Documentos.Services;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.GestionPermisos.DTOs;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Tests.Auditoria;

// Una prueba por cada tipo de evento auditado (RF-AUD-02 a 05), con las piezas reales sobre bases
// en memoria de nombre único y sin levantar la aplicación [RD-12]. Cada una verifica todos los
// campos del registro (RF-AUD-01), incluidos el valor anterior y el nuevo.
public class EventosAuditadosTests
{
    private readonly string _sufijo = Guid.NewGuid().ToString("N");

    private ControlAccesoDbContext ControlAcceso() =>
        new(new DbContextOptionsBuilder<ControlAccesoDbContext>().UseInMemoryDatabase($"A4-CA-{_sufijo}").Options);

    private GestionPermisosDbContext GestionPermisos() =>
        new(new DbContextOptionsBuilder<GestionPermisosDbContext>().UseInMemoryDatabase($"A4-GP-{_sufijo}")
            .ConfigureWarnings(a => a.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private DocumentosDbContext Documentos() =>
        new(new DbContextOptionsBuilder<DocumentosDbContext>().UseInMemoryDatabase($"A4-DOC-{_sufijo}").Options);

    private sealed class CorreoCapturado : ISolicitudCorreoSaliente
    {
        public string UltimoCuerpo { get; private set; } = string.Empty;

        public Task SolicitarEnvioAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken = default)
        {
            UltimoCuerpo = cuerpoHtml;
            return Task.CompletedTask;
        }
    }

    private sealed class AlmacenEnMemoria : IAlmacenArchivos
    {
        private readonly Dictionary<string, byte[]> _archivos = [];

        public async Task GuardarAsync(string rutaRelativa, Stream contenido, CancellationToken cancelacion = default)
        {
            using var copia = new MemoryStream();
            await contenido.CopyToAsync(copia, cancelacion);
            _archivos[rutaRelativa] = copia.ToArray();
        }

        public Stream? Abrir(string rutaRelativa) => _archivos.TryGetValue(rutaRelativa, out var b) ? new MemoryStream(b) : null;

        public void Borrar(string rutaRelativa) => _archivos.Remove(rutaRelativa);
    }

    private async Task<Guid> UsuarioAsync(int rolId, string email = "")
    {
        await using var contexto = ControlAcceso();
        contexto.Database.EnsureCreated();
        var usuario = new Usuario
        {
            Email = string.IsNullOrEmpty(email) ? $"{Guid.NewGuid():N}@prueba.local" : email,
            NombreCompleto = "Usuario de prueba",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Anterior2026", 4),
            RolId = rolId,
            CorreoConfirmado = true
        };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }

    private static async Task<RegistroAuditoria> UnicoRegistroAsync(DbContext contexto)
    {
        var registros = await contexto.Set<RegistroAuditoria>().ToListAsync();
        return Assert.Single(registros);
    }

    private static void TieneTodosLosCampos(RegistroAuditoria registro)
    {
        Assert.NotEqual(Guid.Empty, registro.Id);
        Assert.False(string.IsNullOrWhiteSpace(registro.Accion));
        Assert.False(string.IsNullOrWhiteSpace(registro.Entidad));
        Assert.False(string.IsNullOrWhiteSpace(registro.EntidadId));
        Assert.Equal(DateTimeKind.Utc, registro.FechaUtc.Kind);
        Assert.InRange(registro.FechaUtc, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        Assert.NotNull(JsonDocument.Parse(registro.ValorAnterior));
        Assert.NotNull(JsonDocument.Parse(registro.ValorNuevo));
    }

    // [RF-AUD-02] Aprobar una solicitud registra quién la aprobó, qué y cuándo, con el estado
    // anterior y el nuevo.
    [Fact]
    public async Task AprobarSolicitud_RegistraQuienAproboQueYCuandoConElEstadoAnteriorYElNuevo()
    {
        // Preparar
        var admin = await UsuarioAsync(Rol.IdAdministrador);
        var solicitante = await UsuarioAsync(Rol.IdEstandar);
        var gp = new SolicitudesPermisoService(GestionPermisos(),
            new AplicadorPermisoPorControlAcceso(new AdministracionUsuariosService(ControlAcceso()), new PermisosUsuarioService(ControlAcceso())));
        var solicitud = await gp.CrearAsync(solicitante, new CreacionSolicitudPermisoDto { Permiso = "LIMITE_HABITOS_AMPLIADO" });

        // Actuar
        await gp.AprobarAsync(solicitud.Id, admin);

        // Verificar
        var registro = await UnicoRegistroAsync(GestionPermisos());
        TieneTodosLosCampos(registro);
        Assert.Equal(admin, registro.UsuarioId);
        Assert.Equal("SolicitudAprobada", registro.Accion);
        Assert.Equal("SolicitudPermiso", registro.Entidad);
        Assert.Equal(solicitud.Id.ToString(), registro.EntidadId);
        Assert.Equal("""{"estado":"Pendiente"}""", registro.ValorAnterior);
        Assert.Contains("\"estado\":\"Aplicada\"", registro.ValorNuevo);
        Assert.Contains("LIMITE_HABITOS_AMPLIADO", registro.ValorNuevo);
    }

    // [RF-AUD-02] Rechazar una solicitud registra quién la rechazó y el motivo.
    [Fact]
    public async Task RechazarSolicitud_RegistraQuienRechazoConElMotivo()
    {
        // Preparar
        var admin = await UsuarioAsync(Rol.IdAdministrador);
        var solicitante = await UsuarioAsync(Rol.IdEstandar);
        var gp = new SolicitudesPermisoService(GestionPermisos(), new GestionPermisos.AplicadorPermisoFalso());
        var solicitud = await gp.CrearAsync(solicitante, new CreacionSolicitudPermisoDto { Permiso = "Administrador" });

        // Actuar
        await gp.RechazarAsync(solicitud.Id, admin, new RechazoSolicitudPermisoDto { Motivo = "No corresponde." });

        // Verificar
        var registro = await UnicoRegistroAsync(GestionPermisos());
        TieneTodosLosCampos(registro);
        Assert.Equal(admin, registro.UsuarioId);
        Assert.Equal("SolicitudRechazada", registro.Accion);
        Assert.Equal("""{"estado":"Pendiente"}""", registro.ValorAnterior);
        Assert.Contains("\"estado\":\"Rechazada\"", registro.ValorNuevo);
        Assert.Contains("No corresponde.", registro.ValorNuevo);
    }

    // [RF-AUD-03, RF-DOC-06] Subir y eliminar un documento genera un registro cada uno.
    [Fact]
    public async Task SubirYEliminarDocumento_GeneranUnRegistroCadaUno()
    {
        // Preparar
        var dueno = Guid.NewGuid();
        var almacen = new AlmacenEnMemoria();
        var subido = await new DocumentosService(Documentos(), almacen, TimeProvider.System).SubirAsync(dueno,
            new ArchivoSubidoDto { Nombre = "informe.txt", Tipo = "text/plain", Tamano = 4, Contenido = new MemoryStream([1, 2, 3, 4]) });

        // Actuar
        await new DocumentosService(Documentos(), almacen, TimeProvider.System).EliminarAsync(subido.Id, dueno, esAdministrador: false);

        // Verificar
        var registros = await Documentos().Set<RegistroAuditoria>().OrderBy(r => r.FechaUtc).ToListAsync();
        Assert.Equal(2, registros.Count);
        var subida = registros.Single(r => r.Accion == "DocumentoSubido");
        TieneTodosLosCampos(subida);
        Assert.Equal(dueno, subida.UsuarioId);
        Assert.Equal("Documento", subida.Entidad);
        Assert.Equal(subido.Id.ToString(), subida.EntidadId);
        Assert.Equal("{}", subida.ValorAnterior); // antes no existía
        Assert.Contains("\"nombre\":\"informe.txt\"", subida.ValorNuevo);
        var eliminacion = registros.Single(r => r.Accion == "DocumentoEliminado");
        TieneTodosLosCampos(eliminacion);
        Assert.Equal("""{"eliminado":false}""", eliminacion.ValorAnterior);
        Assert.Contains("\"eliminado\":true", eliminacion.ValorNuevo);
    }

    // [RF-AUD-04] El cambio de rol muestra el rol anterior y el nuevo.
    [Fact]
    public async Task CambiarRol_RegistraElRolAnteriorYElNuevo()
    {
        // Preparar
        var admin = await UsuarioAsync(Rol.IdAdministrador);
        var usuario = await UsuarioAsync(Rol.IdEstandar);

        // Actuar
        await new AdministracionUsuariosService(ControlAcceso()).CambiarRolAsync(usuario, admin, new CambioRolSolicitudDto { Rol = "Administrador" });

        // Verificar
        var registro = await UnicoRegistroAsync(ControlAcceso());
        TieneTodosLosCampos(registro);
        Assert.Equal(admin, registro.UsuarioId);
        Assert.Equal("RolCambiado", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(usuario.ToString(), registro.EntidadId);
        Assert.Equal("""{"rol":"Estandar"}""", registro.ValorAnterior);
        Assert.Equal("""{"rol":"Administrador"}""", registro.ValorNuevo);
    }

    // [RF-AUD-04] Desactivar y reactivar muestran el estado anterior y el nuevo de la cuenta.
    [Fact]
    public async Task DesactivarYReactivar_RegistranElEstadoAnteriorYElNuevoDeLaCuenta()
    {
        // Preparar
        var admin = await UsuarioAsync(Rol.IdAdministrador);
        var usuario = await UsuarioAsync(Rol.IdEstandar);
        await new AdministracionUsuariosService(ControlAcceso()).DesactivarAsync(usuario, admin);

        // Actuar
        await new AdministracionUsuariosService(ControlAcceso()).ReactivarAsync(usuario, admin);

        // Verificar
        var registros = await ControlAcceso().Set<RegistroAuditoria>().ToListAsync();
        var desactivacion = registros.Single(r => r.Accion == "UsuarioDesactivado");
        TieneTodosLosCampos(desactivacion);
        Assert.Equal(admin, desactivacion.UsuarioId);
        Assert.Equal("""{"cuentaHabilitada":true}""", desactivacion.ValorAnterior);
        Assert.Equal("""{"cuentaHabilitada":false}""", desactivacion.ValorNuevo);
        var reactivacion = registros.Single(r => r.Accion == "UsuarioReactivado");
        TieneTodosLosCampos(reactivacion);
        Assert.Equal(admin, reactivacion.UsuarioId);
        Assert.Equal("""{"cuentaHabilitada":false}""", reactivacion.ValorAnterior);
        Assert.Equal("""{"cuentaHabilitada":true}""", reactivacion.ValorNuevo);
    }

    // [RF-AUD-05] Un restablecimiento con el código de recuperación queda como iniciado por el propio
    // usuario y nunca guarda la contraseña, su hash ni el código.
    [Fact]
    public async Task RestablecerConCodigo_QuedaComoIniciadoPorElUsuarioSinContrasenaNiCodigo()
    {
        // Preparar
        var usuario = await UsuarioAsync(Rol.IdEstandar, "ana@prueba.local");
        var correo = new CorreoCapturado();
        await new ContrasenaService(ControlAcceso(), correo).SolicitarRecuperacionAsync(new RecuperacionSolicitudDto { Email = "ana@prueba.local" });
        var codigo = Regex.Match(correo.UltimoCuerpo, @"[A-Z0-9]{4}-[A-Z0-9]{4}").Value;

        // Actuar
        await new ContrasenaService(ControlAcceso(), correo).RestablecerAsync(
            new RestablecimientoSolicitudDto { Email = "ana@prueba.local", Codigo = codigo, ContrasenaNueva = "Nueva2026x" });

        // Verificar
        var registro = await UnicoRegistroAsync(ControlAcceso());
        TieneTodosLosCampos(registro);
        Assert.Equal(usuario, registro.UsuarioId);
        Assert.Equal("RestablecimientoContrasenaPropio", registro.Accion);
        Assert.Equal(usuario.ToString(), registro.EntidadId);
        Assert.Contains("\"iniciadoPor\":\"Usuario\"", registro.ValorNuevo);
        var hash = (await ControlAcceso().Usuarios.SingleAsync(u => u.Id == usuario)).PasswordHash;
        foreach (var secreto in new[] { codigo, codigo.Replace("-", ""), "Nueva2026x", "Anterior2026", hash })
        {
            Assert.DoesNotContain(secreto, registro.ValorAnterior + registro.ValorNuevo);
        }
    }

    // [RF-AUD-05] Un restablecimiento forzado queda como iniciado por el Administrador que lo hizo.
    [Fact]
    public async Task ForzarRestablecimiento_QuedaComoIniciadoPorElAdministrador()
    {
        // Preparar
        var admin = await UsuarioAsync(Rol.IdAdministrador);
        var usuario = await UsuarioAsync(Rol.IdEstandar);
        var correo = new CorreoCapturado();

        // Actuar
        await new ContrasenaService(ControlAcceso(), correo).ForzarRestablecimientoAsync(usuario, admin);

        // Verificar
        var registro = await UnicoRegistroAsync(ControlAcceso());
        TieneTodosLosCampos(registro);
        Assert.Equal(admin, registro.UsuarioId);
        Assert.Equal("RestablecimientoContrasenaForzado", registro.Accion);
        Assert.Equal(usuario.ToString(), registro.EntidadId);
        Assert.Equal("""{"contrasena":"anterior vigente"}""", registro.ValorAnterior);
        Assert.Contains("\"iniciadoPor\":\"Administrador\"", registro.ValorNuevo);
        var codigo = Regex.Match(correo.UltimoCuerpo, @"[A-Z0-9]{4}-[A-Z0-9]{4}").Value;
        Assert.DoesNotContain(codigo, registro.ValorNuevo);
    }
}
