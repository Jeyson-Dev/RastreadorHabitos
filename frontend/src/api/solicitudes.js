import { pedir } from './cliente';

// Solicitudes de permiso (Gestión de permisos). Un Estándar ve solo las suyas pendientes; al
// resolverse dejan de aparecer y llega una notificación.
export const PERMISO_LIMITE_AMPLIADO = 'LIMITE_HABITOS_AMPLIADO';
export const CLAVES_SOLICITUDES = { pendientes: ['solicitudes-permiso', 'pendientes'] };

export const listarSolicitudesPendientes = (senal) => pedir('/api/solicitudes-permiso/pendientes', { senal });

export const crearSolicitudPermiso = (permiso) => pedir('/api/solicitudes-permiso', { metodo: 'POST', cuerpo: { permiso } });

export const aprobarSolicitud = (id) => pedir(`/api/solicitudes-permiso/${encodeURIComponent(id)}/aprobar`, { metodo: 'POST' });

export const rechazarSolicitud = (id, motivo) =>
  pedir(`/api/solicitudes-permiso/${encodeURIComponent(id)}/rechazar`, { metodo: 'POST', cuerpo: { motivo } });
