namespace RastreadorHabitos.Core.Reportes.DTOs;

public class ReporteDto
{
    public string Reporte { get; set; } = string.Empty;
    public string Pregunta { get; set; } = string.Empty;

    // El rango aplicado, con los dos días incluidos; null es un lado abierto.
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }

    // «Todos los usuarios» (Administrador) o «Solo sus datos» (Estándar) [RF-REP-04].
    public string Alcance { get; set; } = string.Empty;

    public List<FilaReporteDto> Filas { get; set; } = [];
}

public class FilaReporteDto
{
    public string Grupo { get; set; } = string.Empty;
    public Dictionary<string, decimal> Valores { get; set; } = [];
}
