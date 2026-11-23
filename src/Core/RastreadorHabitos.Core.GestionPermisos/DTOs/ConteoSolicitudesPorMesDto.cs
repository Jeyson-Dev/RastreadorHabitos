namespace RastreadorHabitos.Core.GestionPermisos.DTOs;

// Solicitudes creadas en un mes, contadas por estado: una fila por mes, nunca una por solicitud
// [RF-REP-01, RF-REP-03].
public class ConteoSolicitudesPorMesDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int Pendiente { get; set; }
    public int Aprobada { get; set; }
    public int Aplicada { get; set; }
    public int Rechazada { get; set; }
    public int Total { get; set; }
}
