using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Data;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public class SesionService : ISesionService
{
    private readonly ControlAccesoDbContext _context;

    public SesionService(ControlAccesoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> EstaAbiertaAsync(Guid sesionId)
    {
        var sesion = await _context.SesionesUsuario
            .AsNoTracking()
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.Id == sesionId);

        // Además de la sesión, se revisa al usuario: uno desactivado o sin correo confirmado
        // no puede seguir usando una credencial emitida antes.
        return sesion is not null
               && sesion.EstaAbierta(DateTime.UtcNow)
               && sesion.Usuario.CorreoConfirmado
               && sesion.Usuario.CuentaHabilitada;
    }
}
