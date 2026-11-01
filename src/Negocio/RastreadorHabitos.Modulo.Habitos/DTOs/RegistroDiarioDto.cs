using RastreadorHabitos.Modulo.Habitos.Entities;

namespace RastreadorHabitos.Modulo.Habitos.DTOs;

public class RegistroDiarioDto
{
    public Guid HabitoId { get; set; }
    public DateOnly Fecha { get; set; }
    public bool Cumplido { get; set; }

    public static RegistroDiarioDto Desde(RegistroDiario registro) => new()
    {
        HabitoId = registro.HabitoId,
        Fecha = registro.Fecha,
        Cumplido = registro.Cumplido
    };
}
