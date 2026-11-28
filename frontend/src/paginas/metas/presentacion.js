import { ESTADOS_META } from '../../api/metas';

// Texto y colores de cada estado de la meta (el estado nunca se comunica solo con color).
export const PRESENTACION_ESTADO = {
  [ESTADOS_META.Pendiente]: { texto: 'Pendiente', chip: 'bg-superficie-suave text-suave', barra: 'bg-suave' },
  [ESTADOS_META.EnProgreso]: { texto: 'En progreso', chip: 'bg-info-suave text-info', barra: 'bg-info' },
  [ESTADOS_META.Completada]: { texto: 'Completada', chip: 'bg-exito-suave text-exito', barra: 'bg-exito' },
  [ESTADOS_META.Fallida]: { texto: 'Fallida', chip: 'bg-peligro-suave text-peligro', barra: 'bg-peligro' },
};
