using RastreadorHabitos.Api.Comandos;
using RastreadorHabitos.Core.Notificaciones.Services;

namespace RastreadorHabitos.Tests.Notificaciones;

// Regresión del Hallazgo 4 de la Asignación 3: con la base de datos detenida, el comando
// enviar-correos (y promover-administrador) terminaba con una excepción sin manejar e imprimía en
// la consola 73 líneas, 55 de ellas de traza, con el texto de la SqlException (RD-08, CT-02).
public class Hallazgo4ComandosSinTrazasTests
{
    // Una excepción con el aspecto de la que lanza SQL Server: mensaje técnico y traza.
    private static Exception ErrorDeBase()
    {
        try
        {
            throw new InvalidOperationException(
                "A network-related or instance-specific error occurred while establishing a connection to SQL Server. Server=sqlserver,1433;Password=secreto");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private sealed class EnviadorQueFalla : IEnviadorColaCorreos
    {
        public Task<ResultadoEnvioCorreos> EnviarPendientesAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<ResultadoEnvioCorreos>(ErrorDeBase());
    }

    // [Hallazgo 4] Si las migraciones fallan porque la base no responde, el comando informa un
    // mensaje comprensible y termina con 1, sin traza ni datos de conexión.
    [Fact]
    public async Task Comando_ConLaBaseCaidaAlMigrar_InformaUnMensajeSinTrazaYTerminaConUno()
    {
        // Preparar
        var salida = new StringWriter();

        // Actuar
        var resultado = await Record.ExceptionAsync(async () =>
        {
            var codigo = await ComandosConsola.EjecutarAsync(() => Task.FromException(ErrorDeBase()), () => Task.FromResult(0), salida);
            Assert.Equal(1, codigo);
        });

        // Verificar
        Assert.Null(resultado);
        var texto = salida.ToString();
        Assert.Contains("No se pudo completar el comando", texto);
        Assert.DoesNotContain(" at ", texto);
        Assert.DoesNotContain("Password", texto);
        Assert.DoesNotContain("SQL Server.", texto);
    }

    // [Hallazgo 4] Si la base deja de responder durante el envío de la cola, pasa lo mismo.
    [Fact]
    public async Task EnviarCorreos_ConLaBaseCaidaDuranteElEnvio_InformaUnMensajeSinTrazaYTerminaConUno()
    {
        // Preparar
        var salida = new StringWriter();

        // Actuar
        var resultado = await Record.ExceptionAsync(async () =>
        {
            var codigo = await ComandosConsola.EjecutarAsync(
                () => Task.CompletedTask, () => ComandosConsola.EnviarCorreosAsync(new EnviadorQueFalla(), salida), salida);
            Assert.Equal(1, codigo);
        });

        // Verificar
        Assert.Null(resultado);
        var texto = salida.ToString();
        Assert.StartsWith("No se pudo completar el comando", texto);
        Assert.DoesNotContain(" at ", texto);
        Assert.DoesNotContain("Password", texto);
    }
}
