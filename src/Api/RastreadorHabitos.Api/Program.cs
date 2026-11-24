using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Api.Adaptadores;
using RastreadorHabitos.Api.Autenticacion;
using RastreadorHabitos.Api.Autorizacion;
using RastreadorHabitos.Api.Comandos;
using RastreadorHabitos.Api.Errores;
using RastreadorHabitos.Core.Auditoria.Data;
using RastreadorHabitos.Core.Auditoria.Services;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.Documentos.Configuracion;
using RastreadorHabitos.Core.Documentos.Data;
using RastreadorHabitos.Core.Documentos.Services;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.GestionPermisos.Services;
using RastreadorHabitos.Core.Notificaciones.Configuracion;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.Services;
using RastreadorHabitos.Core.Reportes.Services;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.Services;
using RastreadorHabitos.Modulo.Metas.Data;
using RastreadorHabitos.Modulo.Metas.Services;

// Comandos independientes: se ejecutan una vez y terminan, sin levantar el servidor web.
//   dotnet run --project src/Api/RastreadorHabitos.Api -- enviar-correos                  [RF-NOT-09]
//   dotnet run --project src/Api/RastreadorHabitos.Api -- promover-administrador <correo>  [RF-CA-04]
const string ComandoEnviarCorreos = "enviar-correos";
const string ComandoPromoverAdministrador = "promover-administrador";
var comando = args.Length > 0 && args[0] is ComandoEnviarCorreos or ComandoPromoverAdministrador ? args[0] : null;
var correoAPromover = comando == ComandoPromoverAdministrador && args.Length > 1 ? args[1] : null;
var argumentosHost = comando switch
{
    ComandoEnviarCorreos => args[1..],
    ComandoPromoverAdministrador => args.Skip(2).ToArray(),
    _ => args
};

var builder = WebApplication.CreateBuilder(argumentosHost);

// La cadena de conexión solo llega por variable de entorno [RD-10]; sin ella la app no arranca.
const string NombreConexion = "RastreadorHabitos";
var cadenaConexion = builder.Configuration.GetConnectionString(NombreConexion);
if (string.IsNullOrWhiteSpace(cadenaConexion))
{
    throw new InvalidOperationException(
        $"Falta la variable de entorno ConnectionStrings__{NombreConexion} con la cadena de conexión a SQL Server.");
}

// Una sola base de datos; cada pieza en su esquema y con su propio historial de migraciones.
builder.Services.AddDbContext<ControlAccesoDbContext>(options =>
    options.UseSqlServer(cadenaConexion,
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", ControlAccesoDbContext.Esquema)));
builder.Services.AddDbContext<NotificacionesDbContext>(options =>
    options.UseSqlServer(cadenaConexion,
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", NotificacionesDbContext.Esquema)));
builder.Services.AddDbContext<MetasDbContext>(options =>
    options.UseSqlServer(cadenaConexion,
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", MetasDbContext.Esquema)));
builder.Services.AddDbContext<GestionPermisosDbContext>(options =>
    options.UseSqlServer(cadenaConexion,
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", GestionPermisosDbContext.Esquema)));
builder.Services.AddDbContext<HabitosDbContext>(options =>
    options.UseSqlServer(cadenaConexion,
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", HabitosDbContext.Esquema)));
builder.Services.AddDbContext<DocumentosDbContext>(options =>
    options.UseSqlServer(cadenaConexion,
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", DocumentosDbContext.Esquema)));
builder.Services.AddDbContext<AuditoriaDbContext>(options =>
    options.UseSqlServer(cadenaConexion,
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", AuditoriaDbContext.Esquema)));

// Los enlaces de los correos se arman con esta URL y nunca con la cabecera Host de la petición.
// Si no se configura, se usa la dirección con la que arranca la aplicación por defecto.
const string UrlBasePorDefecto = "http://localhost:5003";
var urlBase = builder.Configuration["App:UrlBase"];
if (string.IsNullOrWhiteSpace(urlBase))
{
    urlBase = UrlBasePorDefecto;
}
if (!Uri.TryCreate(urlBase, UriKind.Absolute, out var uriBase)
    || (uriBase.Scheme != Uri.UriSchemeHttp && uriBase.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException(
        "La variable de entorno App__UrlBase no es una URL http(s) absoluta (por ejemplo http://localhost:5003).");
}
builder.Services.AddSingleton(new OpcionesEnlaces { UrlBase = urlBase.TrimEnd('/') });

builder.Services.AddScoped<IColaCorreos, ColaCorreos>();
builder.Services.AddScoped<ISolicitudCorreoSaliente, SolicitudCorreoSalientePorCola>();
builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddScoped<IAdministracionUsuariosService, AdministracionUsuariosService>();
builder.Services.AddScoped<IContrasenaService, ContrasenaService>();
builder.Services.AddScoped<IPermisosUsuarioService, PermisosUsuarioService>();
builder.Services.AddScoped<IAplicadorPermiso, AplicadorPermisoPorControlAcceso>();
builder.Services.AddScoped<ISolicitudesPermisoService, SolicitudesPermisoService>();

// Notificaciones: notificar deja la interna y encola el correo; el correo del destinatario y
// quiénes son los Administradores activos los responde Control de acceso por los adaptadores.
builder.Services.AddScoped<IDirectorioUsuarios, DirectorioUsuariosService>();
builder.Services.AddScoped<IDirectorioDestinatarios, DirectorioDestinatariosPorControlAcceso>();
builder.Services.AddScoped<INotificador, Notificador>();
builder.Services.AddScoped<INotificacionesService, NotificacionesService>();
builder.Services.AddScoped<IAvisoSolicitudes, AvisoSolicitudesPorNotificaciones>();
builder.Services.AddScoped<IConsultaColaCorreos, ConsultaColaCorreos>();

// Módulo de negocio. "Hoy" sale del reloj del servidor, nunca de la petición.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<HabitosService>();
builder.Services.AddScoped<IHabitosService>(servicios => servicios.GetRequiredService<HabitosService>());
builder.Services.AddScoped<IConsultaHabitos>(servicios => servicios.GetRequiredService<HabitosService>());
builder.Services.AddScoped<IMetasService, MetasService>();
builder.Services.AddScoped<IMetricasHabitos, MetricasHabitosService>();

// Reportes: cada fuente entrega sus métricas ya agregadas y la pieza aplica el rol [RF-REP-01 a 05].
builder.Services.AddScoped<IFuenteReporte, FuenteSolicitudesPorEstado>();
builder.Services.AddScoped<IFuenteReporte, FuenteCumplimientoSemanal>();
builder.Services.AddScoped<IReportesService, ReportesService>();

// Historial de auditoría: solo consulta [RF-AUD-07]. Las piezas escriben su registro desde sus propios
// contextos, en el mismo guardado que su cambio.
builder.Services.AddScoped<IHistorialAuditoria, HistorialAuditoria>();

// API externa de clima (WeatherAPI.com) para el plan de hoy. La clave solo llega por la variable
// de entorno WeatherApi__Clave [RD-10]; sin ella la API arranca igual y solo esa operación se
// rechaza de forma controlada. La llamada tiene un tiempo máximo para no colgar la petición.
builder.Services.AddSingleton(new OpcionesWeatherApi { Clave = builder.Configuration["WeatherApi:Clave"] });
builder.Services.AddHttpClient<IProveedorPronostico, PronosticoPorWeatherApi>(http =>
{
    http.BaseAddress = new Uri(OpcionesWeatherApi.UrlBase);
    http.Timeout = OpcionesWeatherApi.TiempoMaximo;
});
builder.Services.AddScoped<IPlanDeHoyService, PlanDeHoyService>();

// Credencial de sesión: la clave para firmarla solo llega por variable de entorno [RD-10].
// Los comandos independientes no la necesitan.
if (comando is null)
{
    var claveSesion = builder.Configuration["Jwt:Clave"];
    if (string.IsNullOrWhiteSpace(claveSesion) || claveSesion.Length < OpcionesSesion.LongitudMinimaClave)
    {
        throw new InvalidOperationException(
            $"Falta la variable de entorno Jwt__Clave (clave para firmar las credenciales de sesión) o tiene menos de {OpcionesSesion.LongitudMinimaClave} caracteres.");
    }
    builder.Services.AgregarCredencialSesion(new OpcionesSesion { Clave = claveSesion });
    builder.Services.AgregarExigenciasDeRol();

    // Carpeta de los archivos de documentos: solo por variable de entorno, para poder montarla
    // como volumen fuera de la imagen [RF-DOC-05]. Sin ella la API no arranca.
    var rutaDocumentos = builder.Configuration["Documentos:Ruta"];
    if (string.IsNullOrWhiteSpace(rutaDocumentos))
    {
        throw new InvalidOperationException(
            "Falta la variable de entorno Documentos__Ruta con la carpeta donde se guardan los archivos de los documentos.");
    }
    builder.Services.AddSingleton(new OpcionesDocumentos { Ruta = rutaDocumentos });
    builder.Services.AddSingleton<IAlmacenArchivos, AlmacenArchivosEnDisco>();
    builder.Services.AddScoped<IDocumentosService, DocumentosService>();
}

// Credenciales SMTP solo por variables de entorno Smtp__* [RF-NOT-13, RD-10].
builder.Services.AddSingleton(builder.Configuration.GetSection("Smtp").Get<OpcionesSmtp>() ?? new OpcionesSmtp());
builder.Services.AddSingleton<ITransporteCorreo, TransporteSmtp>();
builder.Services.AddScoped<IEnviadorColaCorreos, EnviadorColaCorreos>();

// Toda la validación de datos vive en los servicios, con mensajes en español [RD-07]:
// por eso se desactiva el "required" implícito que .NET agrega a los string no anulables.
builder.Services
    .AddControllers(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        // La exigencia de rol de cada operación sale de un único punto [RF-CA-05].
        options.Conventions.Add(new AplicarExigenciasDeRol());
    })
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(new { error = "La solicitud no tiene un formato válido." }));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorErroresGlobal>();

var app = builder.Build();

// Comandos independientes (enviar-correos, promover-administrador): aplican las migraciones,
// ejecutan su acción, escriben el resultado en la consola y terminan con su código de salida.
if (comando is not null)
{
    using var scope = app.Services.CreateScope();
    var servicios = scope.ServiceProvider;
    Environment.ExitCode = await ComandosConsola.EjecutarAsync(
        () => ComandosConsola.MigrarAsync(servicios),
        () => comando == ComandoEnviarCorreos
            ? ComandosConsola.EnviarCorreosAsync(servicios.GetRequiredService<IEnviadorColaCorreos>(), Console.Out)
            : ComandosConsola.PromoverAdministradorAsync(servicios.GetRequiredService<IAdministracionUsuariosService>(),
                                                          correoAPromover, Console.Out),
        Console.Out);
    return;
}

// La API: cada pieza aplica sus propias migraciones; la base se crea si no existe [RD-09]. Si no
// se puede, el detalle queda en el log del servidor y la API no arranca.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await ComandosConsola.MigrarAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex,
            "No se pudieron aplicar las migraciones. Revise la variable ConnectionStrings__{NombreConexion} y que SQL Server esté accesible.",
            NombreConexion);
        throw;
    }
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

// Cabeceras de seguridad en todas las respuestas. La CSP solo permite scripts propios (el script
// del tema es un archivo, no va en línea); los estilos en línea los necesitan React y las gráficas.
const string PoliticaContenido =
    "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
    "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
app.Use(async (contexto, siguiente) =>
{
    var cabeceras = contexto.Response.Headers;
    cabeceras.ContentSecurityPolicy = PoliticaContenido;
    cabeceras.XContentTypeOptions = "nosniff";
    cabeceras["Referrer-Policy"] = "no-referrer";
    cabeceras.XFrameOptions = "DENY";
    await siguiente();
});

// El frontend compilado (frontend/ → wwwroot) se sirve en el mismo puerto: sin CORS.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Una ruta /api que no existe responde un 404 en JSON, nunca la página del frontend.
app.Map("/api/{**resto}", () => Results.NotFound(new { error = "No existe esa operación de la API." }));

// Cualquier otra ruta sin archivo es una pantalla del frontend: la resuelve React Router.
app.MapFallbackToFile("index.html");

// Construye los endpoints ya, no en la primera petición: si alguna operación no declara su
// exigencia de rol en ExigenciasDeRol, la API no arranca [RF-CA-05].
_ = ((IEndpointRouteBuilder)app).DataSources.SelectMany(fuente => fuente.Endpoints).ToList();

app.Run();
