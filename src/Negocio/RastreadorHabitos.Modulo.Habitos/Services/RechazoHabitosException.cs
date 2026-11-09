namespace RastreadorHabitos.Modulo.Habitos.Services;

public enum TipoRechazoHabitos
{
    DatosInvalidos,
    NoEncontrado,
    LimiteAlcanzado,
    ReglaIncumplida,

    // El servicio de clima externo no está configurado o no respondió.
    ServicioExternoNoDisponible
}

// Rechazo esperado de una regla de Hábitos. Su mensaje está pensado para el usuario; cualquier
// otra excepción se trata como error interno y nunca se muestra [RD-07, RD-08].
public class RechazoHabitosException : Exception
{
    public TipoRechazoHabitos Tipo { get; }

    public RechazoHabitosException(TipoRechazoHabitos tipo, string mensaje) : base(mensaje)
    {
        Tipo = tipo;
    }
}
