using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.Auditoria.Data;

namespace RastreadorHabitos.Core.Auditoria.Services;

// Un registro del historial tal como lo ve el Administrador. Los valores se devuelven como JSON, no
// como texto.
public class RegistroAuditoriaDto
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string EntidadId { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
    public JsonElement ValorAnterior { get; set; }
    public JsonElement ValorNuevo { get; set; }
}

public class HistorialAuditoriaDto
{
    public int Total { get; set; }
    public List<RegistroAuditoriaDto> Registros { get; set; } = [];
}

public class RechazoAuditoriaException : Exception
{
    public RechazoAuditoriaException(string mensaje) : base(mensaje)
    {
    }
}

// La única operación del historial que se expone: consultarlo. No hay ninguna para editarlo ni
// borrarlo [RF-AUD-06]. Quién puede invocarla (solo el Administrador) lo declara la tabla única de
// exigencias de rol de la Api [RF-AUD-07, RD-06].
public interface IHistorialAuditoria
{
    // Filtros opcionales, como llegan en la consulta: usuarioId (quien actuó), entidad, desde y
    // hasta (AAAA-MM-DD, días locales, los dos incluidos). Un filtro inválido se rechaza [RD-07].
    Task<HistorialAuditoriaDto> ConsultarAsync(string? usuarioId, string? entidad, string? desde, string? hasta);
}

public class HistorialAuditoria : IHistorialAuditoria
{
    private const int LongitudMaximaEntidad = 60;
    private static readonly DateOnly FechaMinima = new(2000, 1, 1);
    private static readonly DateOnly FechaMaxima = new(2100, 12, 31);

    private readonly AuditoriaDbContext _context;
    private readonly TimeProvider _reloj;

    public HistorialAuditoria(AuditoriaDbContext context, TimeProvider reloj)
    {
        _context = context;
        _reloj = reloj;
    }

    public async Task<HistorialAuditoriaDto> ConsultarAsync(string? usuarioId, string? entidad, string? desde, string? hasta)
    {
        // Toda la validación antes de consultar: un filtro inválido nunca llega a la base [RD-07].
        Guid? usuario = null;
        if (!string.IsNullOrWhiteSpace(usuarioId))
        {
            usuario = Guid.TryParse(usuarioId.Trim(), out var id)
                ? id
                : throw new RechazoAuditoriaException("El filtro «usuarioId» debe ser un identificador de usuario (un GUID).");
        }

        var entidadFiltro = string.IsNullOrWhiteSpace(entidad) ? null : entidad.Trim();
        if (entidadFiltro is not null
            && (entidadFiltro.Length > LongitudMaximaEntidad || !entidadFiltro.All(char.IsLetter)))
        {
            throw new RechazoAuditoriaException(
                $"El filtro «entidad» es un nombre de hasta {LongitudMaximaEntidad} letras, por ejemplo Usuario, SolicitudPermiso o Documento.");
        }

        var inicio = LeerFecha(desde, "desde");
        var fin = LeerFecha(hasta, "hasta");
        if (inicio is { } i && fin is { } f && i > f)
        {
            throw new RechazoAuditoriaException("La fecha «desde» no puede ser posterior a «hasta».");
        }

        // El filtro lo resuelve SQL Server: nunca se trae el historial para filtrarlo aquí [RF-AUD-07].
        var zona = _reloj.LocalTimeZone;
        var consulta = _context.RegistrosAuditoria.AsNoTracking();
        if (usuario is { } u)
        {
            consulta = consulta.Where(r => r.UsuarioId == u);
        }
        if (entidadFiltro is not null)
        {
            consulta = consulta.Where(r => r.Entidad == entidadFiltro);
        }
        if (inicio is { } d)
        {
            var desdeUtc = InicioDelDiaUtc(d, zona);
            consulta = consulta.Where(r => r.FechaUtc >= desdeUtc);
        }
        if (fin is { } h)
        {
            // El último día completo está incluido: hasta el comienzo del día siguiente, excluido.
            var hastaUtc = InicioDelDiaUtc(h.AddDays(1), zona);
            consulta = consulta.Where(r => r.FechaUtc < hastaUtc);
        }

        var registros = await consulta.OrderByDescending(r => r.FechaUtc).ToListAsync();
        return new HistorialAuditoriaDto
        {
            Total = registros.Count,
            Registros = registros.Select(r => new RegistroAuditoriaDto
            {
                Id = r.Id,
                UsuarioId = r.UsuarioId,
                Accion = r.Accion,
                Entidad = r.Entidad,
                EntidadId = r.EntidadId,
                FechaUtc = r.FechaUtc,
                ValorAnterior = JsonSerializer.Deserialize<JsonElement>(r.ValorAnterior),
                ValorNuevo = JsonSerializer.Deserialize<JsonElement>(r.ValorNuevo)
            }).ToList()
        };
    }

    private static DateOnly? LeerFecha(string? texto, string nombre)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(texto.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            throw new RechazoAuditoriaException(
                $"La fecha «{nombre}» debe tener el formato AAAA-MM-DD y ser una fecha que existe (por ejemplo 2026-11-01).");
        }

        if (fecha < FechaMinima || fecha > FechaMaxima)
        {
            throw new RechazoAuditoriaException(
                $"La fecha «{nombre}» debe estar entre {FechaMinima:yyyy-MM-dd} y {FechaMaxima:yyyy-MM-dd}.");
        }

        return fecha;
    }

    // Primer instante de un día local en UTC; si esa medianoche no existe (el reloj se adelanta a
    // las 00:00), el primer instante válido. Misma regla que los reportes [RD-11].
    private static DateTime InicioDelDiaUtc(DateOnly dia, TimeZoneInfo zona)
    {
        var local = dia.ToDateTime(TimeOnly.MinValue);
        while (zona.IsInvalidTime(local))
        {
            local = local.AddMinutes(15);
        }
        return TimeZoneInfo.ConvertTimeToUtc(local, zona);
    }
}
