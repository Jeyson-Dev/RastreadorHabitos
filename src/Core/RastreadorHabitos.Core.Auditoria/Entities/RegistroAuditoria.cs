using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace RastreadorHabitos.Core.Auditoria.Entities;

// Quién cambió qué y cuándo [RF-AUD-01]: usuario que actuó, acción, entidad afectada, su
// identificador, fecha y hora, valor anterior y valor nuevo. Nace completo y no se modifica: no
// tiene ninguna operación de edición y el historial rechaza cualquier cambio o borrado [RF-AUD-06].
//
// Auditoría no conoce a ninguna pieza ni al negocio: entidad e identificador son texto, y los
// valores son JSON con solo los campos que cambian [RD-03].
public class RegistroAuditoria
{
    // Usuario que actuó cuando el cambio no lo hizo nadie con sesión (por ejemplo, el comando de
    // consola promover-administrador).
    public static readonly Guid Sistema = Guid.Empty;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UsuarioId { get; private set; }
    public string Accion { get; private set; } = string.Empty;
    public string Entidad { get; private set; } = string.Empty;
    public string EntidadId { get; private set; } = string.Empty;
    public DateTime FechaUtc { get; private set; }

    // JSON con los campos que cambian, antes y después. «{}» si antes no existía (por ejemplo, al
    // subir un documento). Nunca lleva contraseñas, hashes, tokens ni códigos [RF-AUD-05].
    public string ValorAnterior { get; private set; } = "{}";
    public string ValorNuevo { get; private set; } = "{}";

    // Para EF Core.
    private RegistroAuditoria()
    {
    }

    public static RegistroAuditoria Crear(Guid usuarioId, string accion, string entidad, string entidadId,
                                          object? anterior, object? nuevo, DateTime ahoraUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accion);
        ArgumentException.ThrowIfNullOrWhiteSpace(entidad);
        ArgumentException.ThrowIfNullOrWhiteSpace(entidadId);

        return new RegistroAuditoria
        {
            UsuarioId = usuarioId,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            FechaUtc = DateTime.SpecifyKind(ahoraUtc, DateTimeKind.Utc),
            ValorAnterior = AJson(anterior),
            ValorNuevo = AJson(nuevo)
        };
    }

    private static string AJson(object? valor) => valor is null ? "{}" : JsonSerializer.Serialize(valor, Json);
}
