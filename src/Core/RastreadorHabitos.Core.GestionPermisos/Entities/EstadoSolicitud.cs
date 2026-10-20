namespace RastreadorHabitos.Core.GestionPermisos.Entities;

// Único lugar donde se declaran los estados de una solicitud de permiso.
// Las transiciones permitidas entre ellos están en Reglas/TransicionesSolicitud [RD-04].
public enum EstadoSolicitud
{
    // Recién creada, esperando que un Administrador la resuelva [RF-GP-01].
    Pendiente,

    // Un Administrador la aprobó [RF-GP-03].
    Aprobada,

    // El sistema aplicó el permiso aprobado [RF-GP-05].
    Aplicada,

    // Un Administrador la rechazó indicando el motivo [RF-GP-04].
    Rechazada
}
