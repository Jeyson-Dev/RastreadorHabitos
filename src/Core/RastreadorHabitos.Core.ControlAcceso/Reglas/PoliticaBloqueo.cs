using RastreadorHabitos.Core.ControlAcceso.Entities;

namespace RastreadorHabitos.Core.ControlAcceso.Reglas;

// Punto único de la regla de bloqueo [RF-CA-19]: tras 5 intentos fallidos consecutivos la
// cuenta queda bloqueada 15 minutos, y un inicio de sesión correcto pone el contador en cero.
public static class PoliticaBloqueo
{
    public const int IntentosMaximos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    // Mientras dure, se rechaza todo intento, aun con la contraseña correcta.
    public static bool EstaBloqueado(Usuario usuario, DateTime ahoraUtc) =>
        usuario.BloqueadoHastaUtc > ahoraUtc;

    public static void RegistrarFallo(Usuario usuario, DateTime ahoraUtc)
    {
        usuario.IntentosFallidosConsecutivos++;

        if (usuario.IntentosFallidosConsecutivos >= IntentosMaximos)
        {
            // El contador se reinicia: al vencer el bloqueo hacen falta otros 5 fallos.
            usuario.BloqueadoHastaUtc = ahoraUtc.Add(DuracionBloqueo);
            usuario.IntentosFallidosConsecutivos = 0;
        }
    }

    public static void RegistrarExito(Usuario usuario)
    {
        usuario.IntentosFallidosConsecutivos = 0;
        usuario.BloqueadoHastaUtc = null;
    }

    public static string MensajeBloqueo(Usuario usuario, DateTime ahoraUtc)
    {
        var restante = (usuario.BloqueadoHastaUtc ?? ahoraUtc) - ahoraUtc;
        var minutos = Math.Max(1, (int)Math.Ceiling(restante.TotalMinutes));
        return $"La cuenta está bloqueada temporalmente por intentos fallidos. Intente de nuevo en {minutos} minuto{(minutos == 1 ? "" : "s")}.";
    }
}
