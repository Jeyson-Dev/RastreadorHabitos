namespace RastreadorHabitos.Core.Documentos.Services;

public enum TipoRechazoDocumentos
{
    DatosInvalidos,
    NoPermitido,
    NoEncontrado
}

// Rechazo esperado de una regla del Manejador de documentos. Su mensaje está pensado para el
// usuario; cualquier otra excepción se trata como error interno y nunca se muestra [RD-07, RD-08].
public class RechazoDocumentosException : Exception
{
    public TipoRechazoDocumentos Tipo { get; }

    public RechazoDocumentosException(TipoRechazoDocumentos tipo, string mensaje) : base(mensaje)
    {
        Tipo = tipo;
    }
}
