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
        // Entrada inválida: rechazo controlado, nunca una excepción sin manejar [RD-07].
        var nombre = habito.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre) || nombre.Length > LongitudMaximaNombre)
        {
            throw DatosInvalidos($"El nombre es obligatorio y tiene como máximo {LongitudMaximaNombre} caracteres.");
        }

        var descripcion = string.IsNullOrWhiteSpace(habito.Descripcion) ? null : habito.Descripcion.Trim();
        if (descripcion?.Length > LongitudMaximaDescripcion)
        {
            throw DatosInvalidos($"La descripción tiene como máximo {LongitudMaximaDescripcion} caracteres.");
        }

        if (habito.Dias is not { Count: > 0 } || !habito.Dias.All(DiasSemana.EsValido))
        {
            throw DatosInvalidos("Indique al menos un día de la semana, del 1 (lunes) al 7 (domingo).");
        }

        // El límite depende de un permiso que concede Gestión de permisos; el módulo lo consulta
        // solo a Control de acceso, con tienePermiso.
        var habitosActuales = await _context.Habitos.CountAsync(h => h.UsuarioId == usuarioId);
        var limite = LimiteHabitos.Ampliado;
        if (habitosActuales >= limite)
        {
            throw new RechazoHabitosException(TipoRechazoHabitos.LimiteAlcanzado, limite == LimiteHabitos.Normal
                ? $"Alcanzó el límite de {LimiteHabitos.Normal} hábitos. Para crear más, solicite el permiso {LimiteHabitos.PermisoAmpliado}."
                : $"Alcanzó el límite de {LimiteHabitos.Ampliado} hábitos.");
        }

        var nuevo = new Habito
        {
            UsuarioId = usuarioId,
            Nombre = nombre,
            Descripcion = descripcion,
            Dias = habito.Dias.Distinct().Select(dia => new HabitoDia { DiaSemana = dia }).ToList()
        };
        _context.Habitos.Add(nuevo);
        await _context.SaveChangesAsync();

        return HabitoDto.Desde(nuevo);
    }

    public async Task<IReadOnlyList<HabitoDto>> ListarAsync(Guid usuarioId)
    {
        var habitos = await _context.Habitos
            .AsNoTracking()
            .Include(h => h.Dias)
            .Where(h => h.UsuarioId == usuarioId)
            .OrderBy(h => h.FechaCreacionUtc)
            .ToListAsync();

        return habitos.Select(HabitoDto.Desde).ToList();
    }

    public async Task<RegistroDiarioDto> RegistrarCumplimientoAsync(Guid usuarioId, Guid habitoId,
                                                                    RegistroCumplimientoDto registro)
    {
        if (registro.Cumplido is not { } cumplido)
        {
            throw DatosInvalidos("Indique si cumplió el hábito (cumplido: true o false).");
        }

        // Un hábito de otro usuario se trata como inexistente: no revela que existe.
        var habito = await _context.Habitos
            .Include(h => h.Dias)
            .FirstOrDefaultAsync(h => h.Id == habitoId && h.UsuarioId == usuarioId)
            ?? throw new RechazoHabitosException(TipoRechazoHabitos.NoEncontrado, "No existe un hábito con ese id.");

        // La fecha la asigna el servidor: solo hoy, y solo si hoy es un día programado.
        var hoy = DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime);
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

    private static RechazoHabitosException DatosInvalidos(string mensaje) =>
        new(TipoRechazoHabitos.DatosInvalidos, mensaje);
}
