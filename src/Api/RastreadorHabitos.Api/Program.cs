using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Api.Adaptadores;
using RastreadorHabitos.Api.Autenticacion;
using RastreadorHabitos.Api.Errores;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.Notificaciones.Configuracion;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.Services;

// "dotnet run --project src/Api/RastreadorHabitos.Api -- enviar-correos" ejecuta solo el
// enviador de la cola y termina, sin levantar el servidor web [RF-NOT-09].
const string ComandoEnviarCorreos = "enviar-correos";
var esComandoEnviarCorreos = args.Length > 0 && args[0] == ComandoEnviarCorreos;

var builder = WebApplication.CreateBuilder(esComandoEnviarCorreos ? args[1..] : args);

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

// Credencial de sesión: la clave para firmarla solo llega por variable de entorno [RD-10].
// El comando enviar-correos no la necesita.
if (!esComandoEnviarCorreos)
{
    var claveSesion = builder.Configuration["Jwt:Clave"];
    if (string.IsNullOrWhiteSpace(claveSesion) || claveSesion.Length < OpcionesSesion.LongitudMinimaClave)
    {
        throw new InvalidOperationException(
            $"Falta la variable de entorno Jwt__Clave (clave para firmar las credenciales de sesión) o tiene menos de {OpcionesSesion.LongitudMinimaClave} caracteres.");
    }
    builder.Services.AgregarCredencialSesion(new OpcionesSesion { Clave = claveSesion });
}

// Credenciales SMTP solo por variables de entorno Smtp__* [RF-NOT-13, RD-10].
builder.Services.AddSingleton(builder.Configuration.GetSection("Smtp").Get<OpcionesSmtp>() ?? new OpcionesSmtp());
builder.Services.AddScoped<IEnviadorColaCorreos, EnviadorColaCorreos>();

// Toda la validación de datos vive en los servicios, con mensajes en español [RD-07]:
// por eso se desactiva el "required" implícito que .NET agrega a los string no anulables.
builder.Services
    .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(new { error = "La solicitud no tiene un formato válido." }));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorErroresGlobal>();

var app = builder.Build();

// Cada pieza aplica sus propias migraciones; la base se crea si no existe [RD-09].
using (var scope = app.Services.CreateScope())
{
    var servicios = scope.ServiceProvider;
    try
    {
        await servicios.GetRequiredService<ControlAccesoDbContext>().Database.MigrateAsync();
        await servicios.GetRequiredService<NotificacionesDbContext>().Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex,
            "No se pudieron aplicar las migraciones. Revise la variable ConnectionStrings__{NombreConexion} y que SQL Server esté accesible.",
            NombreConexion);
        throw;
    }
}

// Comando independiente: envía los correos pendientes una vez y termina. Ejecutarlo dos veces
// no duplica envíos [RF-NOT-12].
if (esComandoEnviarCorreos)
{
    using var scope = app.Services.CreateScope();
    var resultado = await scope.ServiceProvider.GetRequiredService<IEnviadorColaCorreos>().EnviarPendientesAsync();

    if (!resultado.SmtpConfigurado)
    {
        Console.WriteLine($"SMTP sin configurar (variables Smtp__*): {resultado.Pendientes} correo(s) siguen pendientes.");
    }
    else if (resultado.MotivoFallo is not null)
    {
        Console.WriteLine($"No se pudo enviar: {resultado.MotivoFallo.TrimEnd('.')}. Correos enviados: {resultado.Enviados}. Pendientes: {resultado.Pendientes}.");
    }
    else
    {
        Console.WriteLine($"Correos enviados: {resultado.Enviados}. Pendientes: {resultado.Pendientes}.");
    }

    Environment.ExitCode = resultado.Exito ? 0 : 1;
    return;
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
