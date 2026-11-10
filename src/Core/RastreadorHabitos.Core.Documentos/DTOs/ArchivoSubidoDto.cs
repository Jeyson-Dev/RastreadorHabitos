namespace RastreadorHabitos.Core.Documentos.DTOs;

// El archivo tal como llega en la petición, sin depender de ASP.NET: la Api lo arma y esta
// pieza lo valida antes de usarlo [RD-07].
public class ArchivoSubidoDto
{
    public string? Nombre { get; init; }
    public string? Tipo { get; init; }
    public long Tamano { get; init; }
    public Stream? Contenido { get; init; }
}
