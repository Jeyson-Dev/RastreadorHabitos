using RastreadorHabitos.Core.Reportes.DTOs;
using RastreadorHabitos.Core.Reportes.Reglas;

namespace RastreadorHabitos.Core.Reportes.Services;

public class ReportesService : IReportesService
{
    public const string AlcanceTodos = "Todos los usuarios";
    public const string AlcancePropio = "Solo sus datos";

    private readonly IReadOnlyDictionary<string, IFuenteReporte> _fuentes;
    private readonly TimeProvider _reloj;

    public ReportesService(IEnumerable<IFuenteReporte> fuentes, TimeProvider reloj)
    {
        _fuentes = fuentes.ToDictionary(f => f.Nombre, StringComparer.OrdinalIgnoreCase);
        _reloj = reloj;
    }

    public async Task<ReporteDto> GenerarAsync(string reporte, Guid consultanteId, bool esAdministrador,
                                               string? desde, string? hasta)
    {
        var fuente = _fuentes.GetValueOrDefault(reporte)
            ?? throw new RechazoReportesException("No existe ese reporte.", TipoRechazoReportes.NoEncontrado);

        var rango = RangoFechas.Leer(desde, hasta);

        // El alcance lo decide esta pieza con el rol vigente, nunca un parámetro de la petición:
        // un Estándar no tiene forma de pedir los datos de otro [RF-REP-04].
        Guid? alcance = esAdministrador ? null : consultanteId;
        var filas = await fuente.CalcularAsync(alcance, rango, _reloj.LocalTimeZone);

        return new ReporteDto
        {
            Reporte = fuente.Nombre,
            Pregunta = fuente.Pregunta,
            Desde = rango.Desde,
            Hasta = rango.Hasta,
            Alcance = esAdministrador ? AlcanceTodos : AlcancePropio,
            Filas = filas.Select(f => new FilaReporteDto { Grupo = f.Grupo, Valores = new(f.Valores) }).ToList()
        };
    }
}
