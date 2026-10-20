namespace RastreadorHabitos.Core.GestionPermisos.DTOs;

// Anulable a propósito: la validación vive en el servicio, con mensajes en español [RD-07].
public class CreacionSolicitudPermisoDto
{
    public string? Permiso { get; set; }
}
