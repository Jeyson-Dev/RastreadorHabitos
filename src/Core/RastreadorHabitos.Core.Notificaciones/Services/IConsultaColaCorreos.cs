using RastreadorHabitos.Core.Notificaciones.DTOs;

namespace RastreadorHabitos.Core.Notificaciones.Services;

// Consulta de la cola de correos. Quién puede invocarla (solo el Administrador) lo declara la
// tabla única de exigencias de rol de la Api [RF-NOT-11, RD-06].
public interface IConsultaColaCorreos
{
    // Del más reciente al más antiguo. Con estado (Pendiente, Enviado o Fallido), solo esos; un
    // estado que no existe se rechaza [RD-07].
    Task<IReadOnlyList<CorreoEnColaDto>> ListarAsync(string? estado);
}
