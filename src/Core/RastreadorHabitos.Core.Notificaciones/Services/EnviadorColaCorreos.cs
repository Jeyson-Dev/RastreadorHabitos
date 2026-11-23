using System.Data;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Notificaciones.Data;
using RastreadorHabitos.Core.Notificaciones.Entities;
using RastreadorHabitos.Core.Notificaciones.Reglas;

namespace RastreadorHabitos.Core.Notificaciones.Services;

// Procesador de la cola. Lo ejecuta un comando independiente de las operaciones que originan los
// correos: ellas solo los dejan en la cola, así terminan bien aunque el servidor de correo no
// responda [RF-NOT-08, RF-NOT-09].
public class EnviadorColaCorreos : IEnviadorColaCorreos
{
    private const int TamanoLote = 20;

    private readonly NotificacionesDbContext _context;
    private readonly ITransporteCorreo _transporte;

    public EnviadorColaCorreos(NotificacionesDbContext context, ITransporteCorreo transporte)
    {
        _context = context;
        _transporte = transporte;
    }

    public async Task<ResultadoEnvioCorreos> EnviarPendientesAsync(CancellationToken cancellationToken = default)
    {
        // Solo se toman correos Pendiente: uno Enviado o Fallido nunca vuelve a salir [RF-NOT-12].
        var pendientes = _context.CorreosEnCola.Where(c => c.Estado == CorreoEnCola.EstadoPendiente);

        // Sin servidor configurado no se consume ningún intento: es un problema de configuración,
        // no del correo.
        if (!_transporte.EstaConfigurado)
        {
            return new ResultadoEnvioCorreos(false, false, 0, 0, 0, await pendientes.CountAsync(cancellationToken), null);
        }

        // Un solo procesador a la vez: si otro (el servicio enviador o una ejecución a mano) tiene
        // la cola, este termina sin enviar nada. Así nunca salen dos veces [RF-NOT-12].
        await using var bloqueo = await BloqueoDelProcesador.TomarAsync(_context, cancellationToken);
        if (!bloqueo.Tomado)
        {
            return new ResultadoEnvioCorreos(true, true, 0, 0, 0, await pendientes.CountAsync(cancellationToken), null);
        }

        // Los pendientes al empezar: cada uno recibe a lo sumo un intento en esta ejecución.
        var ids = await pendientes.OrderBy(c => c.FechaCreacionUtc).Select(c => c.Id).ToListAsync(cancellationToken);

        int enviados = 0, conError = 0, pasaronAFallido = 0;
        string? ultimoError = null;

        foreach (var lote in ids.Chunk(TamanoLote))
        {
            var correos = await _context.CorreosEnCola
                .Where(c => lote.Contains(c.Id) && c.Estado == CorreoEnCola.EstadoPendiente)
                .OrderBy(c => c.FechaCreacionUtc)
                .ToListAsync(cancellationToken);

            foreach (var correo in correos)
            {
                var error = await IntentarAsync(correo, cancellationToken);
                if (error is null)
                {
                    correo.RegistrarEnvio(DateTime.UtcNow);
                    enviados++;
                }
                else
                {
                    // Un correo que falla no detiene a los demás: cada uno suma su intento
                    // [RF-NOT-10].
                    correo.RegistrarFallo(error, DateTime.UtcNow);
                    conError++;
                    ultimoError = error;
                    if (correo.Estado == CorreoEnCola.EstadoFallido)
                    {
                        pasaronAFallido++;
                    }
                }

                // Se guarda después de cada correo, no al final: si el proceso se detiene, los
                // ya enviados no se repiten en la siguiente ejecución.
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return new ResultadoEnvioCorreos(true, false, enviados, conError, pasaronAFallido,
                                         await pendientes.CountAsync(cancellationToken), ultimoError);
    }

    // Devuelve null si salió, o el motivo comprensible del fallo [RD-08].
    private async Task<string?> IntentarAsync(CorreoEnCola correo, CancellationToken cancellationToken)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(ReglasCola.TiempoMaximoEnvio);
        try
        {
            await _transporte.EnviarAsync(correo.Destinatario, correo.Asunto, correo.CuerpoHtml, limite.Token);
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return $"El servidor de correo no respondió en {ReglasCola.TiempoMaximoEnvio.TotalSeconds} segundos.";
        }
        catch (FalloEnvioCorreoException fallo)
        {
            return fallo.Message;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Cualquier otro error del transporte es un fallo de ESTE correo: suma su intento y la
            // cola sigue con los demás. Nunca detiene al procesador ni bloquea los correos que
            // vienen detrás [Hallazgo 1 de la Asignación 3, RF-NOT-10]. El detalle técnico no se
            // guarda: el último error lo ve el Administrador [RD-08].
            return $"No se pudo enviar el correo por un error inesperado del transporte ({ex.GetType().Name}).";
        }
    }

    // Bloqueo de aplicación de SQL Server, de sesión y sin espera. Fuera de SQL Server (las pruebas
    // con la base en memoria, que corren un solo procesador) no hace falta.
    private sealed class BloqueoDelProcesador : IAsyncDisposable
    {
        private const string Recurso = "RastreadorHabitos.EnviadorColaCorreos";

        private readonly NotificacionesDbContext? _context;

        private BloqueoDelProcesador(NotificacionesDbContext? context, bool tomado)
        {
            _context = context;
            Tomado = tomado;
        }

        public bool Tomado { get; }

        public static async Task<BloqueoDelProcesador> TomarAsync(NotificacionesDbContext context,
                                                                   CancellationToken cancellationToken)
        {
            if (!context.Database.IsRelational())
            {
                return new BloqueoDelProcesador(null, true);
            }

            await context.Database.OpenConnectionAsync(cancellationToken);
            await using var comando = context.Database.GetDbConnection().CreateCommand();
            comando.CommandText =
                "DECLARE @resultado int; " +
                $"EXEC @resultado = sp_getapplock @Resource = N'{Recurso}', @LockMode = N'Exclusive', " +
                "@LockOwner = N'Session', @LockTimeout = 0; SELECT @resultado;";
            var resultado = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));

            // 0 o 1: tomado. Negativo: otro procesador lo tiene.
            if (resultado >= 0)
            {
                return new BloqueoDelProcesador(context, true);
            }

            await context.Database.CloseConnectionAsync();
            return new BloqueoDelProcesador(null, false);
        }

        public async ValueTask DisposeAsync()
        {
            if (_context is null)
            {
                return;
            }

            var conexion = _context.Database.GetDbConnection();
            if (conexion.State == ConnectionState.Open)
            {
                await using var comando = conexion.CreateCommand();
                comando.CommandText = $"EXEC sp_releaseapplock @Resource = N'{Recurso}', @LockOwner = N'Session';";
                await comando.ExecuteNonQueryAsync();
            }
            await _context.Database.CloseConnectionAsync();
        }
    }
}
