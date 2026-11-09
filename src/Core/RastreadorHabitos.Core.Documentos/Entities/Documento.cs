namespace RastreadorHabitos.Core.Documentos.Entities;

// Documento subido por un usuario. Atributos mínimos del Core: nombre, tipo, tamaño, fecha de
// subida, usuario que lo subió, ruta de almacenamiento y marca de eliminado [RF-DOC-01].
public class Documento
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // Nombre original del archivo, sin carpetas.
    public string Nombre { get; private set; } = string.Empty;

    // Tipo de contenido (MIME) con el que se subió.
    public string Tipo { get; private set; } = string.Empty;

    public long TamanoBytes { get; private set; }

    public DateTime FechaSubidaUtc { get; private set; }

    // Quién lo subió: solo el Id que entrega Control de acceso; esta pieza no conoce sus tablas.
    public Guid SubidoPorId { get; private set; }

    // Dónde quedó el archivo, relativa a la carpeta de documentos (Documentos__Ruta). Nunca
    // depende del nombre que mandó el usuario.
    public string RutaAlmacenamiento { get; private set; } = string.Empty;

    // Borrado lógico: el registro y el archivo siguen existiendo [RF-DOC-04].
    public bool Eliminado { get; private set; }
    public DateTime? FechaEliminacionUtc { get; private set; }

    // Para EF Core.
    private Documento()
    {
    }

    public Documento(string nombre, string tipo, long tamanoBytes, Guid subidoPorId, DateTime ahoraUtc)
    {
        Nombre = nombre;
        Tipo = tipo;
        TamanoBytes = tamanoBytes;
        SubidoPorId = subidoPorId;
        FechaSubidaUtc = ahoraUtc;
        RutaAlmacenamiento = Id.ToString("N");
    }

    public void MarcarEliminado(DateTime ahoraUtc)
    {
        Eliminado = true;
        FechaEliminacionUtc = ahoraUtc;
    }
}
