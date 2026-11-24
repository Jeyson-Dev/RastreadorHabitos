import { instanteUtc } from './fechas';

const relativo = new Intl.RelativeTimeFormat('es', { numeric: 'auto' });
const fechaHora = new Intl.DateTimeFormat('es', { dateStyle: 'medium', timeStyle: 'short' });

const UNIDADES = [
  ['year', 365 * 24 * 3600],
  ['month', 30 * 24 * 3600],
  ['week', 7 * 24 * 3600],
  ['day', 24 * 3600],
  ['hour', 3600],
  ['minute', 60],
];

// «hace 5 minutos», «ayer», «hace 3 semanas» a partir de un instante UTC del servidor.
export function haceCuanto(textoUtc, ahora = new Date()) {
  const instante = instanteUtc(textoUtc);
  if (!instante) return '';
  const segundos = Math.round((instante - ahora) / 1000);
  for (const [unidad, tamano] of UNIDADES) {
    if (Math.abs(segundos) >= tamano) return relativo.format(Math.round(segundos / tamano), unidad);
  }
  return 'hace un momento';
}

// Fecha y hora completas en la zona del navegador (para el título o una tabla).
export function fechaHoraLocal(textoUtc) {
  const instante = instanteUtc(textoUtc);
  return instante ? fechaHora.format(instante) : '';
}
