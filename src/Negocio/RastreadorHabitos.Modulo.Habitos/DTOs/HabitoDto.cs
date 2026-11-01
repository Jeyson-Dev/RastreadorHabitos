using RastreadorHabitos.Modulo.Habitos.Entities;

namespace RastreadorHabitos.Modulo.Habitos.DTOs;

public class HabitoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public List<int> Dias { get; set; } = [];
    public DateTime FechaCreacionUtc { get; set; }

    public static HabitoDto Desde(Habito habito) => new()
    {
        Id = habito.Id,
        Nombre = habito.Nombre,
        Descripcion = habito.Descripcion,
        Dias = habito.Dias.Select(d => d.DiaSemana).Order().ToList(),
        FechaCreacionUtc = habito.FechaCreacionUtc
    };
}
