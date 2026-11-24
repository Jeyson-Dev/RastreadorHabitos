import { pedir } from './cliente';

// Bandeja de notificaciones propias y la preferencia de recibirlas también por correo.
export const TIPOS_NOTIFICACION = {
  SolicitudPermisoCreada: 'SolicitudPermisoCreada',
  SolicitudPermisoAprobada: 'SolicitudPermisoAprobada',
  SolicitudPermisoRechazada: 'SolicitudPermisoRechazada',
  MetaCambioEstado: 'MetaCambioEstado',
};

export const CLAVES_NOTIFICACIONES = {
  bandeja: (soloNoLeidas) => ['notificaciones', 'bandeja', soloNoLeidas ? 'no-leidas' : 'todas'],
  noLeidas: ['notificaciones', 'bandeja', 'no-leidas'],
  preferencias: ['notificaciones', 'preferencias'],
};

export const listarNotificaciones = (soloNoLeidas, senal) =>
  pedir('/api/notificaciones', { consulta: { leida: soloNoLeidas ? false : undefined }, senal });

export const marcarLeida = (id) => pedir(`/api/notificaciones/${encodeURIComponent(id)}/leida`, { metodo: 'POST' });

export const obtenerPreferencias = (senal) => pedir('/api/notificaciones/preferencias', { senal });

export const actualizarPreferencias = (correoActivo) =>
  pedir('/api/notificaciones/preferencias', { metodo: 'PUT', cuerpo: { correoActivo } });
