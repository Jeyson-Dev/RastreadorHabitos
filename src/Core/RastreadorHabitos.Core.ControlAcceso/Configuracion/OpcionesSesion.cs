namespace RastreadorHabitos.Core.ControlAcceso.Configuracion;

// Credencial de sesión: JWT firmado con una clave que solo llega por variable de entorno
// (Jwt__Clave) y nunca está en el repositorio.
public class OpcionesSesion
{
    public const string Emisor = "RastreadorHabitos";
    public const string Audiencia = "RastreadorHabitos";
    public const int LongitudMinimaClave = 32;

    // Únicos datos que lleva la credencial: ni correo, ni hashes, ni nada sensible.
    public const string ClaimUsuario = "sub";
    public const string ClaimSesion = "sid";
    public const string ClaimRol = "role";

    public required string Clave { get; init; }
    public TimeSpan Duracion { get; init; } = TimeSpan.FromHours(8);
}
