namespace RastreadorHabitos.Core.Documentos.Services;

// Dónde viven los bytes de cada documento. El registro (nombre, tipo, dueño...) vive en la base.
public interface IAlmacenArchivos
{
    Task GuardarAsync(string rutaRelativa, Stream contenido, CancellationToken cancelacion = default);

    // Devuelve null si el archivo ya no está en el almacenamiento.
    Stream? Abrir(string rutaRelativa);

    void Borrar(string rutaRelativa);
}
