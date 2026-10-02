namespace RastreadorHabitos.Core.ControlAcceso.Services;

public enum MotivoRechazo
{
    DatosInvalidos,
    Conflicto,
    NoAutenticado,
    NoPermitido,
    Bloqueado,
    NoEncontrado
}

// Rechazo esperado de una regla de Control de acceso. Su mensaje está pensado para el
// usuario; cualquier otra excepción se trata como error interno y nunca se muestra.
public class RechazoControlAccesoException : Exception
{
    public MotivoRechazo Motivo { get; }

    public RechazoControlAccesoException(MotivoRechazo motivo, string mensaje) : base(mensaje)
    {
        Motivo = motivo;
    }
}
