namespace RastreadorHabitos.Core.Documentos.Configuracion;

// Se llena desde la variable de entorno Documentos__Ruta: así la carpeta puede ser un volumen
// montado fuera de la imagen del contenedor [RF-DOC-05].
public class OpcionesDocumentos
{
    public required string Ruta { get; init; }
}
