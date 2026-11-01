using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Modulo.Habitos.Services;
using RastreadorHabitos.Modulo.Metas.Data;
using RastreadorHabitos.Modulo.Metas.DTOs;
using RastreadorHabitos.Modulo.Metas.Entities;

namespace RastreadorHabitos.Modulo.Metas.Services;

public class MetasService : IMetasService
{
    private const int LongitudMaximaNombre = 150;

    private readonly MetasDbContext _context;
    private readonly IConsultaHabitos _habitos;
    private readonly TimeProvider _reloj;

    public MetasService(MetasDbContext context, IConsultaHabitos habitos, TimeProvider reloj)
    {
        _context = context;
        _habitos = habitos;
        _reloj = reloj;
    }

    public async Task<MetaDto> CrearAsync(Guid usuarioId, CreacionMetaDto meta)
    {
        var hoy = Hoy();

        // Entrada inválida: rechazo controlado, nunca una excepción sin manejar [RD-07].
        var nombre = meta.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre) || nombre.Length > LongitudMaximaNombre)
        {
            throw DatosInvalidos($"El nombre es obligatorio y tiene como máximo {LongitudMaximaNombre} caracteres.");
        }

        if (meta.ObjetivoCumplimientos is not >= 1)
        {
            throw DatosInvalidos("El objetivo de cumplimientos es obligatorio y debe ser 1 o más.");
        }

        if (meta.FechaLimite is not { } fechaLimite || fechaLimite < hoy)
        {
            throw DatosInvalidos("La fecha límite es obligatoria (AAAA-MM-DD) y no puede ser anterior a hoy.");
        }

        var habitoIds = meta.HabitoIds?.Distinct().ToList() ?? [];
        if (habitoIds.Count == 0)
        {
            throw DatosInvalidos("Indique al menos un hábito para la meta.");
        }

        // Solo hábitos del propio usuario; los de otro se tratan como inexistentes.
        var propios = await _habitos.FiltrarDelUsuarioAsync(usuarioId, habitoIds);
        if (propios.Count != habitoIds.Count)
        {
            throw DatosInvalidos("Alguno de los hábitos indicados no existe.");
        }

        var nueva = new Meta
        {
            UsuarioId = usuarioId,
            Nombre = nombre,
            ObjetivoCumplimientos = meta.ObjetivoCumplimientos.Value,
            FechaInicio = hoy,
            FechaLimite = fechaLimite,
            Habitos = habitoIds.Select(id => new MetaHabito { HabitoId = id }).ToList()
        };
        _context.Metas.Add(nueva);
        await _context.SaveChangesAsync();

        return await ActualizarYDescribirAsync(nueva, hoy);
    }

    public async Task<IReadOnlyList<MetaDto>> ListarAsync(Guid usuarioId)
    {
        var hoy = Hoy();
        var metas = await _context.Metas
            .Include(m => m.Habitos)
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.FechaCreacionUtc)
            .ToListAsync();

        var resultado = new List<MetaDto>();
        foreach (var meta in metas)
        {
            resultado.Add(await ActualizarYDescribirAsync(meta, hoy));
        }
        return resultado;
    }

    public async Task<MetaDto> ObtenerAsync(Guid usuarioId, Guid metaId) =>
        await ActualizarYDescribirAsync(await BuscarAsync(usuarioId, metaId), Hoy());

    public async Task<MetaDto> CompletarAsync(Guid usuarioId, Guid metaId)
    {
        var hoy = Hoy();
        var meta = await BuscarActualizadaAsync(usuarioId, metaId, hoy);

        meta.Completar(await AvanceAsync(meta), hoy);
        await _context.SaveChangesAsync();

        return await ActualizarYDescribirAsync(meta, hoy);
    }

    public async Task<MetaDto> ReactivarAsync(Guid usuarioId, Guid metaId, ReactivacionMetaDto reactivacion)
    {
        if (reactivacion.NuevaFechaLimite is not { } nuevaFechaLimite)
        {
            throw DatosInvalidos("La nueva fecha límite es obligatoria (AAAA-MM-DD).");
        }

        var hoy = Hoy();
        var meta = await BuscarActualizadaAsync(usuarioId, metaId, hoy);

        meta.Reactivar(nuevaFechaLimite, await AvanceAsync(meta), hoy);
        await _context.SaveChangesAsync();

        return await ActualizarYDescribirAsync(meta, hoy);
    }

    // Busca la meta y aplica las transiciones del sistema antes de que el dueño actúe sobre ella.
    private async Task<Meta> BuscarActualizadaAsync(Guid usuarioId, Guid metaId, DateOnly hoy)
    {
        var meta = await BuscarAsync(usuarioId, metaId);
        await ActualizarYDescribirAsync(meta, hoy);
        return meta;
    }

    // El sistema mueve la meta según su avance y su fecha límite, guarda si cambió y la describe.
    private async Task<MetaDto> ActualizarYDescribirAsync(Meta meta, DateOnly hoy)
    {
        var avance = await AvanceAsync(meta);
        meta.AplicarTransicionesDelSistema(avance, hoy);
        await _context.SaveChangesAsync();
        return MetaDto.Desde(meta, avance);
    }

    // contarCumplidos de Hábitos, entre la fecha de inicio y la fecha límite de la meta.
    private Task<int> AvanceAsync(Meta meta) =>
        _habitos.ContarCumplidosAsync(meta.Habitos.Select(h => h.HabitoId).ToList(), meta.FechaInicio, meta.FechaLimite);

    // Una meta de otro usuario se trata como inexistente: no revela que existe.
    private async Task<Meta> BuscarAsync(Guid usuarioId, Guid metaId) =>
        await _context.Metas
            .Include(m => m.Habitos)
            .FirstOrDefaultAsync(m => m.Id == metaId && m.UsuarioId == usuarioId)
        ?? throw new RechazoMetasException(TipoRechazoMetas.NoEncontrado, "No existe una meta con ese id.");

    private DateOnly Hoy() => DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime);

    private static RechazoMetasException DatosInvalidos(string mensaje) =>
        new(TipoRechazoMetas.DatosInvalidos, mensaje);
}
