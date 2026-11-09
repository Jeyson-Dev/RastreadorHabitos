using RastreadorHabitos.Core.Documentos.Configuracion;

namespace RastreadorHabitos.Core.Documentos.Services;

// Guarda cada archivo en la carpeta de Documentos__Ruta, con el nombre que le asigna la pieza
// (nunca el que manda el usuario), así ninguna subida puede escribir fuera de esa carpeta.
public class AlmacenArchivosEnDisco : IAlmacenArchivos
{
    private readonly string _carpeta;

    public AlmacenArchivosEnDisco(OpcionesDocumentos opciones)
    {
        _carpeta = Path.GetFullPath(opciones.Ruta);
    }

    public async Task GuardarAsync(string rutaRelativa, Stream contenido, CancellationToken cancelacion = default)
    {
        Directory.CreateDirectory(_carpeta);
        await using var destino = new FileStream(RutaCompleta(rutaRelativa), FileMode.CreateNew, FileAccess.Write);
        await contenido.CopyToAsync(destino, cancelacion);
    }

    public Stream? Abrir(string rutaRelativa)
    {
        var ruta = RutaCompleta(rutaRelativa);
        return File.Exists(ruta) ? new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
    }

    public void Borrar(string rutaRelativa)
    {
        File.Delete(RutaCompleta(rutaRelativa));
    }

    private string RutaCompleta(string rutaRelativa) => Path.Combine(_carpeta, Path.GetFileName(rutaRelativa));
}
