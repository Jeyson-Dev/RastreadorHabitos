namespace RastreadorHabitos.Modulo.Habitos.Services;

// Lo que Metas consulta a Hábitos. La dependencia va en un solo sentido: Hábitos no conoce Metas.
public interface IConsultaHabitos
{
    // De los hábitos indicados, los que pertenecen al usuario.
    Task<IReadOnlyList<Guid>> FiltrarDelUsuarioAsync(Guid usuarioId, IReadOnlyCollection<Guid> habitoIds);

    // contarCumplidos: días cumplidos de esos hábitos entre las dos fechas, incluidas.
    Task<int> ContarCumplidosAsync(IReadOnlyCollection<Guid> habitoIds, DateOnly desde, DateOnly hasta);
}
