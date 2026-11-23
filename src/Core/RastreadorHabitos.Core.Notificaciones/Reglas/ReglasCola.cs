namespace RastreadorHabitos.Core.Notificaciones.Reglas;

// Reglas de la cola de correos, en un solo lugar. El máximo de intentos está declarado en el
// README (sección Notificaciones) [RF-NOT-10].
public static class ReglasCola
{
    // Cada ejecución del procesador hace a lo sumo un intento por correo. Al tercer intento
    // fallido el correo queda Fallido y no se vuelve a intentar.
    public const int MaximoIntentos = 3;

    // Espera máxima por correo antes de contarlo como fallido.
    public static readonly TimeSpan TiempoMaximoEnvio = TimeSpan.FromSeconds(10);

    public const int LongitudMaximaError = 1000;
}
