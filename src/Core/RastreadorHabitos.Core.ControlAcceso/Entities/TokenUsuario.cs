namespace RastreadorHabitos.Core.ControlAcceso.Entities;

public class TokenUsuario
{
    public const string TipoActivacionCuenta = "ActivacionCuenta";

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    // Solo se guarda el hash SHA-256 del token; el valor plano viaja únicamente en el correo.
    public string TokenHash { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;

    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracionUtc { get; set; }

    // Un solo uso: deja de ser válido al usarse [RF-CA-16] o al ser reemplazado por un reenvío [RF-CA-17].
    public DateTime? FechaUsoUtc { get; set; }
    public DateTime? FechaRevocacionUtc { get; set; }

    public bool EsValido(DateTime ahoraUtc) =>
        FechaUsoUtc is null && FechaRevocacionUtc is null && FechaExpiracionUtc > ahoraUtc;
}
