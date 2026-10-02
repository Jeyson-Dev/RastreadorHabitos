using System.Security.Cryptography;

namespace RastreadorHabitos.Core.ControlAcceso.Reglas;

// Código de un solo uso con vencimiento para definir una contraseña nueva [RF-CA-10].
// 8 caracteres de 32 posibles (unos 10^12 códigos): no se puede adivinar probando.
public static class CodigoRecuperacion
{
    // Sin 0/O ni 1/I, que se confunden al leerlos en un correo.
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public const int Longitud = 8;
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(30);

    // Se muestra en dos bloques para leerlo con facilidad: K7Q2-M9XP.
    public static string Generar()
    {
        var caracteres = new char[Longitud];
        for (var i = 0; i < Longitud; i++)
        {
            caracteres[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        }
        var codigo = new string(caracteres);
        return $"{codigo[..4]}-{codigo[4..]}";
    }

    // Se acepta con o sin guion, con espacios y en minúsculas; el hash se calcula sobre esta forma.
    public static string Normalizar(string? codigo) =>
        new string((codigo ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}
