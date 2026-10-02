using System.Text;

namespace RastreadorHabitos.Core.ControlAcceso.Reglas;

// Punto único de la política de contraseña [RF-CA-14]: se usa en el registro y en todo
// cambio de contraseña (recuperación, restablecimiento forzado y cambio con sesión).
public static class PoliticaContrasena
{
    public const int LongitudMinima = 8;

    // BCrypt ignora todo lo que pase de 72 bytes: dos contraseñas que solo difieran
    // después de ese punto serían equivalentes.
    public const int BytesMaximos = 72;

    public const string MensajeIncumplimiento =
        "La contraseña debe tener al menos 8 caracteres e incluir letras y números.";
    public const string MensajeDemasiadoLarga =
        "La contraseña no puede superar los 72 caracteres.";

    // Devuelve null si cumple; si no, el mensaje controlado para el usuario [RD-07].
    public static string? Validar(string? contrasena)
    {
        if (string.IsNullOrEmpty(contrasena)
            || contrasena.Length < LongitudMinima
            || !contrasena.Any(char.IsLetter)
            || !contrasena.Any(char.IsAsciiDigit))
        {
            return MensajeIncumplimiento;
        }

        if (Encoding.UTF8.GetByteCount(contrasena) > BytesMaximos)
        {
            return MensajeDemasiadoLarga;
        }

        return null;
    }
}
