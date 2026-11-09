using RastreadorHabitos.Modulo.Habitos.Services;

namespace RastreadorHabitos.Modulo.Habitos.Reglas;

// Regla del módulo: si el pronóstico del día favorece hacer los hábitos al aire libre y qué se le
// recomienda al usuario según los hábitos que le quedan pendientes hoy.
public static class ClimaParaHabitos
{
    // Desde esta probabilidad de lluvia (%) el día ya no se considera favorable.
    public const int ProbabilidadLluviaDesfavorable = 50;
    public const decimal TemperaturaMaximaAgradableC = 33m;
    public const decimal TemperaturaMinimaAgradableC = 10m;

    public static bool EsFavorableAlAireLibre(PronosticoDia pronostico) =>
        pronostico.ProbabilidadLluvia < ProbabilidadLluviaDesfavorable
        && pronostico.TemperaturaMaximaC is >= TemperaturaMinimaAgradableC and <= TemperaturaMaximaAgradableC;

    public static string Recomendacion(PronosticoDia pronostico, int programadosHoy, int pendientesHoy)
    {
        if (programadosHoy == 0)
        {
            return "Hoy no tiene hábitos programados.";
        }

        if (pendientesHoy == 0)
        {
            return "Ya registró todos sus hábitos de hoy.";
        }

        var pendientes = pendientesHoy == 1 ? "1 hábito pendiente" : $"{pendientesHoy} hábitos pendientes";
        if (pronostico.ProbabilidadLluvia >= ProbabilidadLluviaDesfavorable)
        {
            return $"Probabilidad de lluvia del {pronostico.ProbabilidadLluvia} %: planifique bajo techo sus {pendientes} de hoy.";
        }

        if (pronostico.TemperaturaMaximaC > TemperaturaMaximaAgradableC)
        {
            return $"Máxima de {pronostico.TemperaturaMaximaC:0.#} °C: haga temprano o bajo techo sus {pendientes} de hoy.";
        }

        if (pronostico.TemperaturaMaximaC < TemperaturaMinimaAgradableC)
        {
            return $"Máxima de {pronostico.TemperaturaMaximaC:0.#} °C: abríguese o haga bajo techo sus {pendientes} de hoy.";
        }

        return $"Buen día para hacer al aire libre sus {pendientes} de hoy.";
    }
}
