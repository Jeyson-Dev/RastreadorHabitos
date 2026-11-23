namespace RastreadorHabitos.Core.Documentos.Reglas;

// Qué audita el Manejador de documentos, en un solo lugar: toda subida y toda eliminación
// [RF-DOC-06, RF-AUD-03].
public static class EventosAuditoria
{
    public const string Entidad = "Documento";

    public const string DocumentoSubido = "DocumentoSubido";
    public const string DocumentoEliminado = "DocumentoEliminado";
}
