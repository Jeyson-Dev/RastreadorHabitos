namespace RastreadorHabitos.Core.Reportes.Services;

public enum TipoRechazoReportes
{
    DatosInvalidos,
    NoEncontrado
}

// Rechazo esperado de una regla de Reportes. Su mensaje está pensado para el usuario; cualquier
// otra excepción se trata como error interno y nunca se muestra [RD-07, RD-08].
public class RechazoReportesException : Exception
{
    public TipoRechazoReportes Tipo { get; }

    public RechazoReportesException(string mensaje, TipoRechazoReportes tipo = TipoRechazoReportes.DatosInvalidos)
        : base(mensaje)
    {
        Tipo = tipo;
    }
}
