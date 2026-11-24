import { diaLocalDe, diaSemana, instanteUtc } from '../../utilidades/fechas';

// Estado de un día en el historial de un hábito.
export const ESTADOS_DIA = {
  cumplido: { texto: 'cumplido', clase: 'bg-acento' },
  'no-cumplido': { texto: 'no cumplido', clase: 'bg-peligro' },
  'sin-registro': { texto: 'sin registrar', clase: 'bg-zinc-300 dark:bg-zinc-600' },
  pendiente: { texto: 'pendiente (hoy)', clase: 'bg-transparent ring-2 ring-inset ring-acento' },
  'no-programado': { texto: 'no programado', clase: 'bg-transparent ring-1 ring-inset ring-borde' },
  'fuera': { texto: 'sin datos', clase: 'bg-transparent' },
};

export function diaDeCreacion(habito) {
  const instante = instanteUtc(habito.fechaCreacionUtc);
  return instante ? diaLocalDe(instante) : null;
}

// registros: Map de AAAA-MM-DD → cumplido. hoy: el día del servidor (hasta del historial).
export function estadoDelDia(dia, { habito, registros, hoy, creacion }) {
  if (registros.has(dia)) return registros.get(dia) ? 'cumplido' : 'no-cumplido';
  if (dia > hoy || (creacion && dia < creacion)) return 'fuera';
  if (!habito.dias.includes(diaSemana(dia))) return 'no-programado';
  return dia === hoy ? 'pendiente' : 'sin-registro';
}

export function mapaRegistros(historial) {
  return new Map((historial?.registros ?? []).map((r) => [r.fecha, r.cumplido]));
}
