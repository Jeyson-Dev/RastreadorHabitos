using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Modulo.Habitos.Data;
using RastreadorHabitos.Modulo.Habitos.DTOs;
using RastreadorHabitos.Modulo.Habitos.Reglas;

namespace RastreadorHabitos.Modulo.Habitos.Services;

public class PlanDeHoyService : IPlanDeHoyService
{
    private const int LongitudMaximaCiudad = 100;

    private readonly HabitosDbContext _context;
    private readonly IProveedorPronostico _pronosticos;
    private readonly TimeProvider _reloj;

    public PlanDeHoyService(HabitosDbContext context, IProveedorPronostico pronosticos, TimeProvider reloj)
    {
        _context = context;
        _pronosticos = pronosticos;
        _reloj = reloj;
    }

    public async Task<PlanDeHoyDto> ObtenerAsync(Guid usuarioId, string? ciudad, CancellationToken cancelacion = default)
    {
        // Entrada inválida: rechazo controlado, antes de llamar al servicio externo [RD-07].
        var ciudadLimpia = ciudad?.Trim();
        if (string.IsNullOrEmpty(ciudadLimpia) || ciudadLimpia.Length > LongitudMaximaCiudad)
        {
            throw new RechazoHabitosException(TipoRechazoHabitos.DatosInvalidos,
                $"Indique la ciudad, de hasta {LongitudMaximaCiudad} caracteres (por ejemplo ?ciudad=Santo Domingo).");
        }

        var resultado = await _pronosticos.ObtenerDeHoyAsync(ciudadLimpia, cancelacion);
        var pronostico = resultado.Estado switch
        {
            EstadoPronostico.Obtenido when resultado.Pronostico is not null => resultado.Pronostico,
            EstadoPronostico.CiudadNoEncontrada => throw new RechazoHabitosException(TipoRechazoHabitos.DatosInvalidos,
                "El servicio de clima no encontró la ciudad indicada."),
            EstadoPronostico.SinClave => throw NoDisponible(
                "El plan de hoy no está disponible: el servidor no tiene configurada la clave del servicio de clima."),
            EstadoPronostico.ClaveRechazada => throw NoDisponible(
                "El plan de hoy no está disponible: el servicio de clima rechazó la clave configurada en el servidor."),
            _ => throw NoDisponible(
                "El plan de hoy no está disponible: el servicio de clima no respondió. Intente de nuevo más tarde.")
        };

        // Hábitos del usuario programados para hoy y lo que ya registró hoy. «Hoy» es la fecha
        // local del servidor, igual que en el registro diario.
        var hoy = DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime);
        var diaHoy = DiasSemana.De(hoy);
        var habitos = await _context.Habitos
            .AsNoTracking()
            .Where(h => h.UsuarioId == usuarioId && h.Dias.Any(d => d.DiaSemana == diaHoy))
            .OrderBy(h => h.FechaCreacionUtc)
            .Select(h => new HabitoDeHoyDto
            {
                Id = h.Id,
                Nombre = h.Nombre,
                Registrado = h.Registros.Any(r => r.Fecha == hoy),
                Cumplido = h.Registros.Where(r => r.Fecha == hoy).Select(r => (bool?)r.Cumplido).FirstOrDefault()
            })
            .ToListAsync(cancelacion);

        var pendientes = habitos.Count(h => !h.Registrado);
        return new PlanDeHoyDto
        {
            Fecha = hoy,
            Ciudad = pronostico.Ciudad,
            Pais = pronostico.Pais,
            Pronostico = new PronosticoDto
            {
                Condicion = pronostico.Condicion,
                TemperaturaMaximaC = pronostico.TemperaturaMaximaC,
                TemperaturaMinimaC = pronostico.TemperaturaMinimaC,
                ProbabilidadLluvia = pronostico.ProbabilidadLluvia
            },
            FavorableAlAireLibre = ClimaParaHabitos.EsFavorableAlAireLibre(pronostico),
            Recomendacion = ClimaParaHabitos.Recomendacion(pronostico, habitos.Count, pendientes),
            Pendientes = pendientes,
            Habitos = habitos
        };
    }

    private static RechazoHabitosException NoDisponible(string mensaje) =>
        new(TipoRechazoHabitos.ServicioExternoNoDisponible, mensaje);
}
