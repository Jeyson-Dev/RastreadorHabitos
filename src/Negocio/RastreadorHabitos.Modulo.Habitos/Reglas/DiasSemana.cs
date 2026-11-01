namespace RastreadorHabitos.Modulo.Habitos.Reglas;

// Los días de la semana del módulo: 1 = lunes ... 7 = domingo.
public static class DiasSemana
{
    public const int Lunes = 1;
    public const int Domingo = 7;

    public static bool EsValido(int dia) => dia is >= Lunes and <= Domingo;

    public static int De(DateOnly fecha) => fecha.DayOfWeek == DayOfWeek.Sunday ? Domingo : (int)fecha.DayOfWeek;
}
