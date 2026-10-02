using Microsoft.AspNetCore.Diagnostics;

namespace RastreadorHabitos.Api.Errores;

// Punto único que decide qué ve el usuario ante un error no controlado [RD-08]:
// el detalle completo queda en el log del servidor, nunca en la respuesta.
public class ManejadorErroresGlobal : IExceptionHandler
{
    private readonly ILogger<ManejadorErroresGlobal> _logger;

    public ManejadorErroresGlobal(ILogger<ManejadorErroresGlobal> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
                                                CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}.",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = "Ocurrió un error interno. Intente de nuevo más tarde." }, cancellationToken);

        return true;
    }
}
