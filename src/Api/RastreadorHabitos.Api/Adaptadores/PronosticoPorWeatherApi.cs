using System.Globalization;
using System.Net;
using System.Text.Json;
using RastreadorHabitos.Modulo.Habitos.Services;

namespace RastreadorHabitos.Api.Adaptadores;

// Clave de WeatherAPI.com: solo llega por la variable de entorno WeatherApi__Clave, nunca desde el
// repositorio, el Dockerfile ni el docker-compose.yml [RD-10].
public class OpcionesWeatherApi
{
    public const string UrlBase = "https://api.weatherapi.com/v1/";
    public static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(5);

    public string? Clave { get; init; }
}

// Conecta el puerto del módulo Hábitos con WeatherAPI.com (forecast.json). Cualquier fallo del
// servicio externo (sin clave, clave rechazada, caído, lento o respuesta inesperada) se informa
// como estado, nunca como excepción: el módulo responde un rechazo controlado [RD-07, RD-08].
public class PronosticoPorWeatherApi : IProveedorPronostico
{
    // Errores de WeatherAPI: https://www.weatherapi.com/docs/#intro-error-codes
    private const int CodigoUbicacionNoEncontrada = 1006;

    private readonly HttpClient _http;
    private readonly OpcionesWeatherApi _opciones;
    private readonly ILogger<PronosticoPorWeatherApi> _logger;

    public PronosticoPorWeatherApi(HttpClient http, OpcionesWeatherApi opciones, ILogger<PronosticoPorWeatherApi> logger)
    {
        _http = http;
        _opciones = opciones;
        _logger = logger;
    }

    public async Task<ResultadoPronostico> ObtenerDeHoyAsync(string ciudad, CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(_opciones.Clave))
        {
            return new ResultadoPronostico(EstadoPronostico.SinClave);
        }

        // La URL lleva la clave: nunca se escribe en el log.
        var ruta = $"forecast.json?key={Uri.EscapeDataString(_opciones.Clave)}&q={Uri.EscapeDataString(ciudad)}" +
                   "&days=1&lang=es&aqi=no&alerts=no";
        try
        {
            using var respuesta = await _http.GetAsync(ruta, cancelacion);
            await using var cuerpo = await respuesta.Content.ReadAsStreamAsync(cancelacion);
            using var json = await JsonDocument.ParseAsync(cuerpo, cancellationToken: cancelacion);

            if (respuesta.IsSuccessStatusCode)
            {
                return ResultadoPronostico.Con(Leer(json.RootElement));
            }

            var codigo = json.RootElement.TryGetProperty("error", out var error)
                         && error.TryGetProperty("code", out var valor) && valor.TryGetInt32(out var numero)
                ? numero
                : 0;
            _logger.LogWarning("WeatherAPI respondió {Estado} con el código de error {Codigo}.",
                (int)respuesta.StatusCode, codigo);

            return respuesta.StatusCode switch
            {
                HttpStatusCode.BadRequest when codigo == CodigoUbicacionNoEncontrada =>
                    new ResultadoPronostico(EstadoPronostico.CiudadNoEncontrada),
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    new ResultadoPronostico(EstadoPronostico.ClaveRechazada),
                _ => new ResultadoPronostico(EstadoPronostico.NoDisponible)
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException or FormatException)
        {
            // Sin conexión, sin respuesta en TiempoMaximo o con una respuesta que no se entiende.
            _logger.LogWarning("WeatherAPI no está disponible: {Tipo}.", ex.GetType().Name);
            return new ResultadoPronostico(EstadoPronostico.NoDisponible);
        }
    }

    private static PronosticoDia Leer(JsonElement raiz)
    {
        var ubicacion = raiz.GetProperty("location");
        var dia = raiz.GetProperty("forecast").GetProperty("forecastday")[0];
        var datos = dia.GetProperty("day");

        return new PronosticoDia(
            Ciudad: ubicacion.GetProperty("name").GetString() ?? string.Empty,
            Pais: ubicacion.GetProperty("country").GetString() ?? string.Empty,
            Fecha: DateOnly.ParseExact(dia.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Condicion: datos.GetProperty("condition").GetProperty("text").GetString() ?? string.Empty,
            TemperaturaMaximaC: datos.GetProperty("maxtemp_c").GetDecimal(),
            TemperaturaMinimaC: datos.GetProperty("mintemp_c").GetDecimal(),
            ProbabilidadLluvia: datos.GetProperty("daily_chance_of_rain").GetInt32());
    }
}
