namespace RastreadorHabitos.Modulo.Habitos.Reglas;

// Rachas de un hábito (regla 4 del diseño): cuentan días programados consecutivos cumplidos. Un día
// programado ya pasado sin cumplir, o registrado como no cumplido, rompe la racha. Los días que no
// están programados no cuentan ni la rompen, y hoy sin registrar todavía no la rompe.
public static class Rachas
{
    public static (int Actual, int Mejor) Calcular(IReadOnlyCollection<int> diasProgramados,
                                                   IReadOnlyDictionary<DateOnly, bool> registros,
                                                   DateOnly desde, DateOnly hoy)
    {
        var actual = 0;
        var mejor = 0;

        for (var fecha = desde; fecha <= hoy; fecha = fecha.AddDays(1))
        {
            if (!diasProgramados.Contains(DiasSemana.De(fecha)))
            {
                continue;
            }

            if (registros.TryGetValue(fecha, out var cumplido) && cumplido)
            {
                actual++;
                mejor = Math.Max(mejor, actual);
            }
            else if (fecha == hoy && !registros.ContainsKey(fecha))
            {
                // Hoy sigue pendiente: la racha se mantiene hasta que el día termine.
                continue;
            }
            else
            {
                actual = 0;
            }
        }

        return (actual, mejor);
    }
}
