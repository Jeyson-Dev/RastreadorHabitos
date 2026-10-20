using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.GestionPermisos.Data;

namespace RastreadorHabitos.Tests.GestionPermisos;

// Ejercita Gestión de permisos sin levantar la aplicación ni SQL Server [RD-12]. Cada prueba
// crea su propia base con un nombre único, así ninguna depende de lo que dejó otra.
public sealed class BaseEnMemoria
{
    private readonly DbContextOptions<GestionPermisosDbContext> _opciones =
        new DbContextOptionsBuilder<GestionPermisosDbContext>()
            .UseInMemoryDatabase($"GestionPermisos-{Guid.NewGuid()}")
            .Options;

    // Cada llamada entrega un contexto nuevo: verificar con uno distinto al que actuó
    // comprueba lo que quedó guardado, no lo que quedó en memoria.
    public GestionPermisosDbContext NuevoContexto() => new(_opciones);
}
