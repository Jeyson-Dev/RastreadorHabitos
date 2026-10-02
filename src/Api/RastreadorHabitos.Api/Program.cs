using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Api.Adaptadores;
using RastreadorHabitos.Api.Errores;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddScoped<IColaCorreos, ColaCorreos>();
builder.Services.AddScoped<ISolicitudCorreoSaliente, SolicitudCorreoSalientePorCola>();

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

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapControllers();

app.Run();
