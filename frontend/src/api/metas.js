import { pedir } from './cliente';

// Metas y su máquina de estados. Las transiciones del sistema (Pendiente → En progreso, vencida →
// Fallida) las aplica el servidor al consultar; el dueño solo completa o reactiva.
export const ESTADOS_META = {
  Pendiente: 'Pendiente',
  EnProgreso: 'EnProgreso',
  Completada: 'Completada',
  Fallida: 'Fallida',
};

export const CLAVES_METAS = { lista: ['metas'] };

export const listarMetas = (senal) => pedir('/api/metas', { senal });

export const crearMeta = ({ nombre, objetivoCumplimientos, fechaLimite, habitoIds }) =>
  pedir('/api/metas', { metodo: 'POST', cuerpo: { nombre, objetivoCumplimientos, fechaLimite, habitoIds } });

export const completarMeta = (id) => pedir(`/api/metas/${encodeURIComponent(id)}/completar`, { metodo: 'POST' });

export const reactivarMeta = (id, nuevaFechaLimite) =>
  pedir(`/api/metas/${encodeURIComponent(id)}/reactivar`, { metodo: 'POST', cuerpo: { nuevaFechaLimite } });
