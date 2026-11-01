namespace RastreadorHabitos.Modulo.Metas.Services;

public enum TipoRechazoMetas
{
    DatosInvalidos,
    NoEncontrado,
    TransicionNoPermitida
}

// Rechazo esperado de una regla de Metas. Su mensaje está pensado para el usuario; cualquier
// otra excepción se trata como error interno y nunca se muestra [RD-07, RD-08].
public class RechazoMetasException : Exception
{
    public TipoRechazoMetas Tipo { get; }

    public RechazoMetasException(TipoRechazoMetas tipo, string mensaje) : base(mensaje)
    {
        Tipo = tipo;
    }
}
