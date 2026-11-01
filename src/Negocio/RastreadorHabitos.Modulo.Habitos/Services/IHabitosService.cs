using RastreadorHabitos.Modulo.Habitos.DTOs;

namespace RastreadorHabitos.Modulo.Habitos.Services;

// Operaciones de Hábitos. Siempre sobre los hábitos del usuario que Control de acceso ya
// identificó; quién puede invocarlas lo declara la tabla única de exigencias de rol de la Api.
public interface IHabitosService
{
    // Hasta 10 hábitos, o 20 con el permiso LIMITE_HABITOS_AMPLIADO, que se consulta a Control
    // de acceso con tienePermiso.
    Task<HabitoDto> CrearAsync(Guid usuarioId, CreacionHabitoDto habito);

    Task<IReadOnlyList<HabitoDto>> ListarAsync(Guid usuarioId);

    // Registra si hoy cumplió el hábito. Solo en un día programado; registrar otra vez el mismo
    // día corrige el registro.
    Task<RegistroDiarioDto> RegistrarCumplimientoAsync(Guid usuarioId, Guid habitoId, RegistroCumplimientoDto registro);
}
