using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Services;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Habitos.Entities;
using RastreadorHabitos.Modulo.Habitos.Reglas;

namespace RastreadorHabitos.Modulo.Habitos.Services;

public class HabitosService : IHabitosService, IConsultaHabitos
{
    private const int LongitudMaximaNombre = 100;
    private const int LongitudMaximaDescripcion = 500;
    private const int DiasPorDefectoHistorial = 30;
    private const int DiasMaximosHistorial = 366;

    private readonly HabitosDbContext _context;
    private readonly IPermisosUsuarioService _permisos;
    private readonly TimeProvider _reloj;

    public HabitosService(HabitosDbContext context, IPermisosUsuarioService permisos, TimeProvider reloj)
    {
        _context = context;
        _permisos = permisos;
        _reloj = reloj;
    }

    public async Task<HabitoDto> CrearAsync(Guid usuarioId, CreacionHabitoDto habito)
    {
        var (nombre, descripcion, dias) = Validar(habito.Nombre, habito.Descripcion, habito.Dias);

        await ExigirLugarEnElLimiteAsync(usuarioId);

        var nuevo = new Habito
        {
            UsuarioId = usuarioId,
            Nombre = nombre,
            Descripcion = descripcion,
            Dias = dias.Select(dia => new HabitoDia { DiaSemana = dia }).ToList()
        };
        _context.Habitos.Add(nuevo);
        await _context.SaveChangesAsync();

        return HabitoDto.Desde(nuevo, Hoy(), new Dictionary<DateOnly, bool>(), (0, 0));
    }

    public async Task<HabitoDto> EditarAsync(Guid usuarioId, Guid habitoId, EdicionHabitoDto edicion)
    {
        var (nombre, descripcion, dias) = Validar(edicion.Nombre, edicion.Descripcion, edicion.Dias);
        var habito = await BuscarPropioAsync(usuarioId, habitoId);

        habito.Nombre = nombre;
        habito.Descripcion = descripcion;

        // Los días se reemplazan: se quitan los que ya no están y se agregan los nuevos.
        habito.Dias.RemoveAll(d => !dias.Contains(d.DiaSemana));
        var nuevos = dias.Where(dia => habito.Dias.All(d => d.DiaSemana != dia))
                         .Select(dia => new HabitoDia { HabitoId = habito.Id, DiaSemana = dia })
                         .ToList();
        habito.Dias.AddRange(nuevos);
        await _context.SaveChangesAsync();

        return await DescribirAsync(habito);
    }

    public async Task<HabitoDto> ArchivarAsync(Guid usuarioId, Guid habitoId)
    {
        var habito = await BuscarPropioAsync(usuarioId, habitoId);
        if (habito.Archivado)
        {
            throw new RechazoHabitosException(TipoRechazoHabitos.ReglaIncumplida, "El hábito ya está archivado.");
        }

        habito.FechaArchivadoUtc = _reloj.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync();

        return await DescribirAsync(habito);
    }

    public async Task<HabitoDto> RestaurarAsync(Guid usuarioId, Guid habitoId)
    {
        var habito = await BuscarPropioAsync(usuarioId, habitoId);
        if (!habito.Archivado)
        {
            throw new RechazoHabitosException(TipoRechazoHabitos.ReglaIncumplida, "El hábito no está archivado.");
        }

        // Volver a activarlo ocupa un lugar del límite, igual que crear uno (regla 2 del diseño).
        await ExigirLugarEnElLimiteAsync(usuarioId);

        habito.FechaArchivadoUtc = null;
        await _context.SaveChangesAsync();

        return await DescribirAsync(habito);
    }

    public async Task EliminarAsync(Guid usuarioId, Guid habitoId)
    {
        // Borra el hábito con sus días y sus registros diarios (regla 3 del diseño). Los registros
        // se borran aquí y no solo por la cascada de la base, para no depender del motor.
        var habito = await BuscarPropioAsync(usuarioId, habitoId);
        _context.RegistrosDiarios.RemoveRange(await _context.RegistrosDiarios.Where(r => r.HabitoId == habitoId).ToListAsync());
        _context.Habitos.Remove(habito);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<HabitoDto>> ListarAsync(Guid usuarioId)
    {
        var habitos = await _context.Habitos
            .AsNoTracking()
            .Include(h => h.Dias)
            .Where(h => h.UsuarioId == usuarioId)
            .OrderBy(h => h.FechaCreacionUtc)
            .ToListAsync();

        var ids = habitos.Select(h => h.Id).ToList();
        var registros = (await _context.RegistrosDiarios
                .AsNoTracking()
                .Where(r => ids.Contains(r.HabitoId))
                .Select(r => new { r.HabitoId, r.Fecha, r.Cumplido })
                .ToListAsync())
            .ToLookup(r => r.HabitoId);

        var hoy = Hoy();
        return habitos.Select(habito =>
        {
            var delHabito = registros[habito.Id].ToDictionary(r => r.Fecha, r => r.Cumplido);
            return HabitoDto.Desde(habito, hoy, delHabito, CalcularRachas(habito, delHabito, hoy));
        }).ToList();
    }

    public async Task<HistorialHabitoDto> ConsultarRegistrosAsync(Guid usuarioId, Guid habitoId, string? desde, string? hasta)
    {
        var hoy = Hoy();
        var fin = LeerFecha(hasta, "hasta") ?? hoy;
        var inicio = LeerFecha(desde, "desde") ?? fin.AddDays(-(DiasPorDefectoHistorial - 1));
        if (inicio > fin)
        {
            throw DatosInvalidos("La fecha desde no puede ser posterior a la fecha hasta.");
        }
        if (fin.DayNumber - inicio.DayNumber + 1 > DiasMaximosHistorial)
        {
            throw DatosInvalidos($"El rango de fechas puede abarcar como máximo {DiasMaximosHistorial} días.");
        }

        // Un hábito de otro usuario se trata como inexistente: no revela que existe.
        var habito = await _context.Habitos
            .AsNoTracking()
            .Include(h => h.Dias)
            .FirstOrDefaultAsync(h => h.Id == habitoId && h.UsuarioId == usuarioId)
            ?? throw new RechazoHabitosException(TipoRechazoHabitos.NoEncontrado, "No existe un hábito con ese id.");

        var registros = await _context.RegistrosDiarios
            .AsNoTracking()
            .Where(r => r.HabitoId == habitoId)
            .ToListAsync();
        var porFecha = registros.ToDictionary(r => r.Fecha, r => r.Cumplido);
        var rachas = CalcularRachas(habito, porFecha, hoy);

        return new HistorialHabitoDto
        {
            HabitoId = habitoId,
            Desde = inicio,
            Hasta = fin,
            RachaActual = rachas.Actual,
            MejorRacha = rachas.Mejor,
            Registros = registros
                .Where(r => r.Fecha >= inicio && r.Fecha <= fin)
                .OrderBy(r => r.Fecha)
                .Select(RegistroDiarioDto.Desde)
                .ToList()
        };
    }

    public async Task<RegistroDiarioDto> RegistrarCumplimientoAsync(Guid usuarioId, Guid habitoId,
                                                                    RegistroCumplimientoDto registro)
    {
        if (registro.Cumplido is not { } cumplido)
        {
            throw DatosInvalidos("Indique si cumplió el hábito (cumplido: true o false).");
        }

        var habito = await BuscarPropioAsync(usuarioId, habitoId);
        if (habito.Archivado)
        {
            throw new RechazoHabitosException(TipoRechazoHabitos.ReglaIncumplida,
                "El hábito está archivado. Restáurelo para registrar su cumplimiento.");
        }

        // La fecha la asigna el servidor: solo hoy, y solo si hoy es un día programado.
        var hoy = Hoy();
        if (habito.Dias.All(d => d.DiaSemana != DiasSemana.De(hoy)))
        {
            throw new RechazoHabitosException(TipoRechazoHabitos.ReglaIncumplida,
                "Hoy no es un día programado para este hábito.");
        }

        // Un solo registro por hábito y día, editable ese mismo día.
        var deHoy = await _context.RegistrosDiarios.FirstOrDefaultAsync(r => r.HabitoId == habitoId && r.Fecha == hoy);
        if (deHoy is null)
        {
            deHoy = new RegistroDiario { HabitoId = habitoId, Fecha = hoy };
            _context.RegistrosDiarios.Add(deHoy);
        }
        deHoy.Cumplido = cumplido;
        await _context.SaveChangesAsync();

        return RegistroDiarioDto.Desde(deHoy);
    }

    public async Task<IReadOnlyList<Guid>> FiltrarDelUsuarioAsync(Guid usuarioId, IReadOnlyCollection<Guid> habitoIds) =>
        await _context.Habitos
            .Where(h => h.UsuarioId == usuarioId && habitoIds.Contains(h.Id))
            .Select(h => h.Id)
            .ToListAsync();

    public async Task<int> ContarCumplidosAsync(IReadOnlyCollection<Guid> habitoIds, DateOnly desde, DateOnly hasta) =>
        await _context.RegistrosDiarios
            .CountAsync(r => habitoIds.Contains(r.HabitoId) && r.Cumplido && r.Fecha >= desde && r.Fecha <= hasta);

    // «Hoy» es el día local del servidor [RD-11].
    private DateOnly Hoy() => DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime);

    // Las rachas se cuentan desde el día local en que se creó el hábito, convertido con las reglas
    // de la zona para esa fecha (no con la diferencia horaria de hoy). Un hábito archivado conserva
    // la racha que tenía: se cuenta hasta el día en que se archivó, que vale como «hoy» (regla 3).
    private (int Actual, int Mejor) CalcularRachas(Habito habito, IReadOnlyDictionary<DateOnly, bool> registros, DateOnly hoy)
    {
        var desde = DiaLocal(habito.FechaCreacionUtc);
        if (registros.Count > 0 && registros.Keys.Min() < desde)
        {
            desde = registros.Keys.Min();
        }

        var hasta = habito.FechaArchivadoUtc is { } archivado && DiaLocal(archivado) < hoy ? DiaLocal(archivado) : hoy;
        var dias = habito.Dias.Select(d => d.DiaSemana).ToHashSet();
        return Rachas.Calcular(dias, registros, desde, hasta);
    }

    // El día local de un instante UTC, con las reglas de la zona para esa fecha [RD-11].
    private DateOnly DiaLocal(DateTime fechaUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc),
                                                              _reloj.LocalTimeZone));

    // Nombre, descripción y días de un hábito, validados para crear o editar. Entrada inválida:
    // rechazo controlado, nunca una excepción sin manejar [RD-07].
    private static (string Nombre, string? Descripcion, List<int> Dias) Validar(string? nombreRecibido,
                                                                              string? descripcionRecibida,
                                                                              List<int>? diasRecibidos)
    {
        var nombre = nombreRecibido?.Trim();
        if (string.IsNullOrEmpty(nombre) || nombre.Length > LongitudMaximaNombre)
        {
            throw DatosInvalidos($"El nombre es obligatorio y tiene como máximo {LongitudMaximaNombre} caracteres.");
        }

        var descripcion = string.IsNullOrWhiteSpace(descripcionRecibida) ? null : descripcionRecibida.Trim();
        if (descripcion?.Length > LongitudMaximaDescripcion)
        {
            throw DatosInvalidos($"La descripción tiene como máximo {LongitudMaximaDescripcion} caracteres.");
        }

        if (diasRecibidos is not { Count: > 0 } || !diasRecibidos.All(DiasSemana.EsValido))
        {
            throw DatosInvalidos("Indique al menos un día de la semana, del 1 (lunes) al 7 (domingo).");
        }

        return (nombre, descripcion, diasRecibidos.Distinct().ToList());
    }

    // El límite cuenta solo los hábitos activos (los archivados no) y depende de un permiso que
    // concede Gestión de permisos; el módulo lo consulta solo a Control de acceso, con tienePermiso.
    private async Task ExigirLugarEnElLimiteAsync(Guid usuarioId)
    {
        var habitosActuales = await _context.Habitos.CountAsync(h => h.UsuarioId == usuarioId && h.FechaArchivadoUtc == null);
        var limite = await _permisos.TienePermisoAsync(usuarioId, LimiteHabitos.PermisoAmpliado) ? LimiteHabitos.Ampliado : LimiteHabitos.Normal;
        if (habitosActuales >= limite)
        {
            throw new RechazoHabitosException(TipoRechazoHabitos.LimiteAlcanzado, limite == LimiteHabitos.Normal
                ? $"Alcanzó el límite de {LimiteHabitos.Normal} hábitos. Para crear más, solicite el permiso {LimiteHabitos.PermisoAmpliado}."
                : $"Alcanzó el límite de {LimiteHabitos.Ampliado} hábitos.");
        }
    }

    // Un hábito de otro usuario se trata como inexistente: no revela que existe [RD-06].
    private async Task<Habito> BuscarPropioAsync(Guid usuarioId, Guid habitoId) =>
        await _context.Habitos
            .Include(h => h.Dias)
            .FirstOrDefaultAsync(h => h.Id == habitoId && h.UsuarioId == usuarioId)
        ?? throw new RechazoHabitosException(TipoRechazoHabitos.NoEncontrado, "No existe un hábito con ese id.");

    // El hábito con su estado de hoy y sus rachas, igual que en el listado.
    private async Task<HabitoDto> DescribirAsync(Habito habito)
    {
        var registros = await _context.RegistrosDiarios
            .AsNoTracking()
            .Where(r => r.HabitoId == habito.Id)
            .ToDictionaryAsync(r => r.Fecha, r => r.Cumplido);
        var hoy = Hoy();
        return HabitoDto.Desde(habito, hoy, registros, CalcularRachas(habito, registros, hoy));
    }

    // Una fecha AAAA-MM-DD opcional: ausente, vacía o en blanco toma el valor por defecto.
    private static DateOnly? LeerFecha(string? valor, string nombre)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }
        if (!DateOnly.TryParseExact(valor.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            throw DatosInvalidos($"La fecha {nombre} debe tener el formato AAAA-MM-DD (por ejemplo 2026-11-23).");
        }
        return fecha;
    }

    private static RechazoHabitosException DatosInvalidos(string mensaje) =>
        new(TipoRechazoHabitos.DatosInvalidos, mensaje);
}
