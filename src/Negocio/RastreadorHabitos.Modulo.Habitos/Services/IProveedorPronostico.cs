namespace RastreadorHabitos.Modulo.Habitos.Services;

// Puerto hacia el servicio de clima externo. El módulo solo conoce este contrato; la Api lo
// conecta con la API real, que lee su clave de una variable de entorno [RD-10].
public interface IProveedorPronostico
{
    // Nunca lanza por un fallo del servicio externo: lo informa en el resultado, para que el
    // módulo responda un rechazo controlado [RD-07, RD-08].
    Task<ResultadoPronostico> ObtenerDeHoyAsync(string ciudad, CancellationToken cancelacion = default);
}

public enum EstadoPronostico
{
    Obtenido,
    SinClave,
    ClaveRechazada,
    CiudadNoEncontrada,
    NoDisponible
}

// Pronóstico del día en la ciudad consultada.
public record PronosticoDia(
    string Ciudad,
    string Pais,
    DateOnly Fecha,
    string Condicion,
    decimal TemperaturaMaximaC,
    decimal TemperaturaMinimaC,
    int ProbabilidadLluvia);

public record ResultadoPronostico(EstadoPronostico Estado, PronosticoDia? Pronostico = null)
{
    public static ResultadoPronostico Con(PronosticoDia pronostico) => new(EstadoPronostico.Obtenido, pronostico);
}
