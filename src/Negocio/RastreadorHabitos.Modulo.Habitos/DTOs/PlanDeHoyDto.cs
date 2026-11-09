namespace RastreadorHabitos.Modulo.Habitos.DTOs;

// Los hábitos programados para hoy cruzados con el pronóstico del día: si el día favorece hacerlos
// al aire libre y qué se recomienda.
public class PlanDeHoyDto
{
    public DateOnly Fecha { get; set; }
    public string Ciudad { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public PronosticoDto Pronostico { get; set; } = new();
    public bool FavorableAlAireLibre { get; set; }
    public string Recomendacion { get; set; } = string.Empty;
    public int Pendientes { get; set; }
    public List<HabitoDeHoyDto> Habitos { get; set; } = [];
}

public class PronosticoDto
{
    public string Condicion { get; set; } = string.Empty;
    public decimal TemperaturaMaximaC { get; set; }
    public decimal TemperaturaMinimaC { get; set; }
    public int ProbabilidadLluvia { get; set; }
}

public class HabitoDeHoyDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    // Si ya tiene registro hoy y, en ese caso, si lo cumplió.
    public bool Registrado { get; set; }
    public bool? Cumplido { get; set; }
}
