using RastreadorHabitos.Modulo.Habitos.Entities;
using RastreadorHabitos.Modulo.Habitos.Reglas;

namespace RastreadorHabitos.Modulo.Habitos.DTOs;

public class HabitoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public List<int> Dias { get; set; } = [];
    public DateTime FechaCreacionUtc { get; set; }
    public bool Archivado { get; set; }
    public DateTime? FechaArchivadoUtc { get; set; }

    // Estado de hoy y rachas, calculados al consultar (no se guardan).
    public int RachaActual { get; set; }
    public int MejorRacha { get; set; }
    public bool ProgramadoHoy { get; set; }

    // null si hoy todavía no se registró.
    public bool? CumplidoHoy { get; set; }

    public static HabitoDto Desde(Habito habito, DateOnly hoy, IReadOnlyDictionary<DateOnly, bool> registros,
                                  (int Actual, int Mejor) rachas) => new()
    {
        Id = habito.Id,
        Nombre = habito.Nombre,
        Descripcion = habito.Descripcion,
        Dias = habito.Dias.Select(d => d.DiaSemana).Order().ToList(),
        // La base devuelve las fechas sin indicar que son UTC: se marcan para que siempre salgan con
        // la misma forma (…Z) [RD-11].
        FechaCreacionUtc = DateTime.SpecifyKind(habito.FechaCreacionUtc, DateTimeKind.Utc),
        Archivado = habito.Archivado,
        FechaArchivadoUtc = habito.FechaArchivadoUtc is { } archivado ? DateTime.SpecifyKind(archivado, DateTimeKind.Utc) : null,
        RachaActual = rachas.Actual,
        MejorRacha = rachas.Mejor,
        ProgramadoHoy = habito.Dias.Any(d => d.DiaSemana == DiasSemana.De(hoy)),
        CumplidoHoy = registros.TryGetValue(hoy, out var cumplido) ? cumplido : null
    };
}
