using System.Globalization;
using RastreadorHabitos.Core.Reportes.Services;

namespace RastreadorHabitos.Core.Reportes.Reglas;

// Rango de fechas de un reporte, en días locales del servidor y con los DOS extremos incluidos
// [RF-REP-05]. Sin «desde» o sin «hasta», ese lado queda abierto.
public sealed record RangoFechas(DateOnly? Desde, DateOnly? Hasta)
{
    public const string Formato = "yyyy-MM-dd";

    // Límites razonables: fuera de ellos la fecha se rechaza, nunca desborda un cálculo [RD-07].
    public static readonly DateOnly FechaMinima = new(2000, 1, 1);
    public static readonly DateOnly FechaMaxima = new(2100, 12, 31);

    public static readonly RangoFechas SinLimites = new(null, null);

    // Entrada de la consulta tal como llega (?desde=AAAA-MM-DD&hasta=AAAA-MM-DD).
    public static RangoFechas Leer(string? desde, string? hasta)
    {
        var inicio = LeerFecha(desde, "desde");
        var fin = LeerFecha(hasta, "hasta");

        if (inicio is { } i && fin is { } f && i > f)
        {
            throw new RechazoReportesException("La fecha «desde» no puede ser posterior a «hasta».");
        }

        return new RangoFechas(inicio, fin);
    }

    // Primer instante incluido, en UTC: el comienzo local de «desde».
    public DateTime? DesdeUtc(TimeZoneInfo zona) => Desde is { } d ? InicioDelDiaUtc(d, zona) : null;

    // Primer instante EXCLUIDO, en UTC: el comienzo local del día siguiente a «hasta». Así todo
    // «hasta», hasta las 23:59:59.999 locales, queda dentro [RF-REP-05].
    public DateTime? HastaExclusivoUtc(TimeZoneInfo zona) => Hasta is { } h ? InicioDelDiaUtc(h.AddDays(1), zona) : null;

    // Primer instante de un día local, en UTC. En las zonas que adelantan el reloj a medianoche
    // (por ejemplo America/Havana) la medianoche de ese día no existe: el día empieza en su primer
    // instante válido, en lugar de fallar [Hallazgo 3 de la Asignación 3, RD-07].
    public static DateTime InicioDelDiaUtc(DateOnly dia, TimeZoneInfo zona)
    {
        var local = dia.ToDateTime(TimeOnly.MinValue);
        while (zona.IsInvalidTime(local))
        {
            local = local.AddMinutes(15);
        }
        return TimeZoneInfo.ConvertTimeToUtc(local, zona);
    }

    private static DateOnly? LeerFecha(string? texto, string nombre)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(texto.Trim(), Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            throw new RechazoReportesException(
                $"La fecha «{nombre}» debe tener el formato AAAA-MM-DD y ser una fecha que existe (por ejemplo 2026-11-01).");
        }

        if (fecha < FechaMinima || fecha > FechaMaxima)
        {
            throw new RechazoReportesException(
                $"La fecha «{nombre}» debe estar entre {FechaMinima:yyyy-MM-dd} y {FechaMaxima:yyyy-MM-dd}.");
        }

        return fecha;
    }
}
