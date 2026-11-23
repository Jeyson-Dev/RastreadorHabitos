using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Data;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Core.Documentos.Data;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.Services;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Metas.Data;

namespace RastreadorHabitos.Api.Comandos;

// Los comandos independientes de la Api (enviar-correos, promover-administrador): se ejecutan una
// vez, escriben su resultado en la consola y terminan con un código de salida. Fuera de Program.cs
// para poder probarlos sin levantar la aplicación [RD-12].
public static class ComandosConsola
{
    public const string EnviarCorreos = "enviar-correos";
    public const string PromoverAdministrador = "promover-administrador";

    // Cada pieza aplica sus propias migraciones; la base se crea si no existe [RD-09].
    public static async Task MigrarAsync(IServiceProvider servicios)
    {
        await servicios.GetRequiredService<ControlAccesoDbContext>().Database.MigrateAsync();
        await servicios.GetRequiredService<NotificacionesDbContext>().Database.MigrateAsync();
        await servicios.GetRequiredService<MetasDbContext>().Database.MigrateAsync();
        await servicios.GetRequiredService<GestionPermisosDbContext>().Database.MigrateAsync();
        await servicios.GetRequiredService<HabitosDbContext>().Database.MigrateAsync();
        await servicios.GetRequiredService<DocumentosDbContext>().Database.MigrateAsync();
    }

    // Aplica las migraciones, ejecuta la acción del comando y devuelve su código de salida.
    public static async Task<int> EjecutarAsync(Func<Task> migrar, Func<Task<int>> accion, TextWriter salida)
    {
        await migrar();
        return await accion();
    }

    // Hace un intento por cada correo pendiente [RF-NOT-09, RF-NOT-10]. Ejecutarlo dos veces no
    // duplica envíos [RF-NOT-12].
    public static async Task<int> EnviarCorreosAsync(IEnviadorColaCorreos enviador, TextWriter salida)
    {
        var resultado = await enviador.EnviarPendientesAsync();

        if (!resultado.SmtpConfigurado)
        {
            await salida.WriteLineAsync($"SMTP sin configurar (variables Smtp__*): {resultado.Pendientes} correo(s) siguen pendientes.");
        }
        else if (resultado.OtroProcesadorEnCurso)
        {
            await salida.WriteLineAsync($"Otro procesador de la cola está en curso: no se envió nada. Pendientes: {resultado.Pendientes}.");
        }
        else
        {
            await salida.WriteLineAsync($"Correos enviados: {resultado.Enviados}. Con error en este intento: {resultado.ConError} " +
                                        $"(pasaron a Fallido: {resultado.PasaronAFallido}). Pendientes: {resultado.Pendientes}.");
            if (resultado.UltimoError is not null)
            {
                await salida.WriteLineAsync($"Último error: {resultado.UltimoError}");
            }
        }

        return resultado.Exito || resultado.OtroProcesadorEnCurso ? 0 : 1;
    }

    // Convierte en Administrador a un usuario ya registrado; así nace el primero [RF-CA-04].
    public static async Task<int> PromoverAdministradorAsync(IAdministracionUsuariosService administracion,
                                                              string? correo, TextWriter salida)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            await salida.WriteLineAsync($"Uso: dotnet run --no-build --project src/Api/RastreadorHabitos.Api -- {PromoverAdministrador} <correo>");
            return 1;
        }

        try
        {
            await administracion.PromoverAdministradorAsync(correo);
            await salida.WriteLineAsync($"El usuario {correo.Trim().ToLowerInvariant()} ahora es Administrador.");
            return 0;
        }
        catch (RechazoControlAccesoException rechazo)
        {
            await salida.WriteLineAsync(rechazo.Message);
            return 1;
        }
    }
}
