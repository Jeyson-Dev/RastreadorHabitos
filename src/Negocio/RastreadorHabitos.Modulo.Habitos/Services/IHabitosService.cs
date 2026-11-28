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

    // Reemplaza nombre, descripción y días de un hábito propio, archivado o no.
    Task<HabitoDto> EditarAsync(Guid usuarioId, Guid habitoId, EdicionHabitoDto edicion);

    // Archivar conserva el historial y las rachas, libera un lugar del límite y deja de aparecer en
    // el plan de hoy; restaurar lo vuelve a activar si el límite lo permite (reglas 2 y 3 del diseño).
    Task<HabitoDto> ArchivarAsync(Guid usuarioId, Guid habitoId);

    Task<HabitoDto> RestaurarAsync(Guid usuarioId, Guid habitoId);

    // Elimina el hábito y sus registros diarios. Las metas descartan el vínculo al consultarlas.
    Task EliminarAsync(Guid usuarioId, Guid habitoId);

    // Solo lectura: los registros de un hábito propio entre dos días locales AAAA-MM-DD (por
    // defecto, los últimos 30 días hasta hoy) y sus rachas.
    Task<HistorialHabitoDto> ConsultarRegistrosAsync(Guid usuarioId, Guid habitoId, string? desde, string? hasta);
}
