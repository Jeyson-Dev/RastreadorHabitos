namespace RastreadorHabitos.Core.GestionPermisos.DTOs;

// Anulable a propósito: sin motivo escrito el rechazo no se completa [RF-GP-04, RD-07].
public class RechazoSolicitudPermisoDto
{
    public string? Motivo { get; set; }
}
