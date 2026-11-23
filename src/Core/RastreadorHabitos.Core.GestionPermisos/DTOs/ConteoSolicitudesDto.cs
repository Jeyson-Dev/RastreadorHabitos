namespace RastreadorHabitos.Core.GestionPermisos.DTOs;

// Un intervalo de tiempo en UTC: desde incluido, hasta excluido. Quien pide el conteo decide los
// intervalos (por ejemplo, los meses locales); esta pieza no sabe de zonas horarias.
public sealed record PeriodoUtc(DateTime DesdeUtc, DateTime HastaUtc);

// Solicitudes creadas en un intervalo, contadas por estado: una fila por intervalo, nunca una por
// solicitud [RF-REP-01, RF-REP-03].
public class ConteoSolicitudesDto
{
    public int Pendiente { get; set; }
    public int Aprobada { get; set; }
    public int Aplicada { get; set; }
    public int Rechazada { get; set; }
    public int Total { get; set; }
}
