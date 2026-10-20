namespace RastreadorHabitos.Core.GestionPermisos.Services;

public enum TipoRechazoGestionPermisos
{
    DatosInvalidos,
    NoPermitido,
    NoEncontrado,
    TransicionNoPermitida
}

// Rechazo esperado de una regla de Gestión de permisos. Su mensaje está pensado para el
// usuario; cualquier otra excepción se trata como error interno y nunca se muestra [RD-07].
public class RechazoGestionPermisosException : Exception
{
    public TipoRechazoGestionPermisos Tipo { get; }

    public RechazoGestionPermisosException(TipoRechazoGestionPermisos tipo, string mensaje) : base(mensaje)
    {
        Tipo = tipo;
    }
}
