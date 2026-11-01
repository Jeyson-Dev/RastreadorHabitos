using RastreadorHabitos.Modulo.Metas.Entities;

namespace RastreadorHabitos.Modulo.Metas.DTOs;

public class MetaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int ObjetivoCumplimientos { get; set; }
    public int Avance { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaLimite { get; set; }
    public string Estado { get; set; } = string.Empty;
    public List<Guid> HabitoIds { get; set; } = [];
    public DateTime FechaCreacionUtc { get; set; }

    public static MetaDto Desde(Meta meta, int avance) => new()
    {
        Id = meta.Id,
        Nombre = meta.Nombre,
        ObjetivoCumplimientos = meta.ObjetivoCumplimientos,
        Avance = avance,
        FechaInicio = meta.FechaInicio,
        FechaLimite = meta.FechaLimite,
        Estado = meta.Estado.ToString(),
        HabitoIds = meta.Habitos.Select(h => h.HabitoId).ToList(),
        FechaCreacionUtc = meta.FechaCreacionUtc
    };
}
