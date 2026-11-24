import { ESTADOS_META } from '../../api/metas';

// Texto y colores de cada estado de la meta (el estado nunca se comunica solo con color).
export const PRESENTACION_ESTADO = {
  [ESTADOS_META.Pendiente]: { texto: 'Pendiente', chip: 'bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300', barra: 'bg-zinc-400' },
  [ESTADOS_META.EnProgreso]: { texto: 'En progreso', chip: 'bg-info-suave text-info', barra: 'bg-info' },
  [ESTADOS_META.Completada]: { texto: 'Completada', chip: 'bg-acento-suave text-acento', barra: 'bg-acento' },
  [ESTADOS_META.Fallida]: { texto: 'Fallida', chip: 'bg-peligro-suave text-peligro', barra: 'bg-peligro' },
};
