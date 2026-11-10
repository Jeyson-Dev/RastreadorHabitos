namespace RastreadorHabitos.Core.Documentos.Reglas;

// Qué se acepta como nombre y tipo de un archivo subido.
public static class ReglasArchivo
{
    public const int LongitudMaximaNombre = 255;
    public const int LongitudMaximaTipo = 255;
    public const string TipoPorDefecto = "application/octet-stream";

    // Solo el nombre, sin carpetas (algunos navegadores mandan la ruta completa). Null si no
    // queda un nombre usable.
    public static string? NormalizarNombre(string? nombre)
    {
        var limpio = Path.GetFileName((nombre ?? string.Empty).Replace('\\', '/')).Trim();
        return limpio.Length is > 0 and <= LongitudMaximaNombre ? limpio : null;
    }

    public static string NormalizarTipo(string? tipo)
    {
        var limpio = (tipo ?? string.Empty).Trim();
        return limpio.Length is > 0 and <= LongitudMaximaTipo ? limpio : TipoPorDefecto;
    }
}
