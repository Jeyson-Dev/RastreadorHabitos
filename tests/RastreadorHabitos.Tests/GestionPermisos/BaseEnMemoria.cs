using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RastreadorHabitos.Core.GestionPermisos.Data;
using RastreadorHabitos.Core.GestionPermisos.Services;

namespace RastreadorHabitos.Tests.GestionPermisos;

// Ejercita Gestión de permisos sin levantar la aplicación ni SQL Server [RD-12]. Cada prueba
// crea su propia base con un nombre único, así ninguna depende de lo que dejó otra.
public sealed class BaseEnMemoria
{
    private readonly DbContextOptions<GestionPermisosDbContext> _opciones =
        new DbContextOptionsBuilder<GestionPermisosDbContext>()
            .UseInMemoryDatabase($"GestionPermisos-{Guid.NewGuid()}")
            // El proveedor en memoria no tiene transacciones: el bloqueo de la fila al aprobar lo da
            // SQL Server. El token de concurrencia sí se comprueba aquí.
            .ConfigureWarnings(avisos => avisos.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    // Cada llamada entrega un contexto nuevo: verificar con uno distinto al que actuó
    // comprueba lo que quedó guardado, no lo que quedó en memoria.
    public GestionPermisosDbContext NuevoContexto() => new(_opciones);

    public SolicitudesPermisoService NuevoServicio(IAplicadorPermiso? aplicador = null) =>
        NuevoServicio(NuevoContexto(), aplicador);

    // Con un contexto dado: sirve para que el servicio trabaje sobre una lectura ya desactualizada.
    public static SolicitudesPermisoService NuevoServicio(GestionPermisosDbContext contexto,
                                                          IAplicadorPermiso? aplicador = null) =>
        new(contexto, aplicador ?? new AplicadorPermisoFalso());
}
