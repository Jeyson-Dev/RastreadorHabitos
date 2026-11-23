namespace RastreadorHabitos.Core.Notificaciones.Services;

public enum TipoRechazoNotificaciones
{
    DatosInvalidos,
    NoPermitido,
    NoEncontrado
}

// Rechazo esperado de una regla de Notificaciones. Su mensaje está pensado para el usuario;
// cualquier otra excepción se trata como error interno y nunca se muestra [RD-07, RD-08].
public class RechazoNotificacionesException : Exception
{
    public TipoRechazoNotificaciones Tipo { get; }

    public RechazoNotificacionesException(TipoRechazoNotificaciones tipo, string mensaje) : base(mensaje)
    {
        Tipo = tipo;
    }
}
