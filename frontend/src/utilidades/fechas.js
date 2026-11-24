// Fechas del dominio. Los días llegan como texto AAAA-MM-DD (día local del servidor) y se operan
// en UTC para no depender de la zona del navegador. Los días de la semana van de 1 (lunes) a 7.

export const DIAS_SEMANA = [
  { numero: 1, corto: 'L', nombre: 'lunes' },
  { numero: 2, corto: 'M', nombre: 'martes' },
  { numero: 3, corto: 'X', nombre: 'miércoles' },
  { numero: 4, corto: 'J', nombre: 'jueves' },
  { numero: 5, corto: 'V', nombre: 'viernes' },
  { numero: 6, corto: 'S', nombre: 'sábado' },
  { numero: 7, corto: 'D', nombre: 'domingo' },
];
export const ENTRE_SEMANA = [1, 2, 3, 4, 5];
export const TODOS_LOS_DIAS = [1, 2, 3, 4, 5, 6, 7];

function aUtc(iso) {
  const [anio, mes, dia] = iso.split('-').map(Number);
  return new Date(Date.UTC(anio, mes - 1, dia));
}

function aIso(fecha) {
  return fecha.toISOString().slice(0, 10);
}

export function sumarDias(iso, dias) {
  const fecha = aUtc(iso);
  fecha.setUTCDate(fecha.getUTCDate() + dias);
  return aIso(fecha);
}

// 1 = lunes … 7 = domingo.
export function diaSemana(iso) {
  const dia = aUtc(iso).getUTCDay();
  return dia === 0 ? 7 : dia;
}

// Todos los días desde «desde» hasta «hasta», incluidos.
export function rangoDias(desde, hasta) {
  const dias = [];
  for (let actual = desde; actual <= hasta; actual = sumarDias(actual, 1)) dias.push(actual);
  return dias;
}

export function primerDiaDelMes(iso) {
  return `${iso.slice(0, 7)}-01`;
}

export function ultimoDiaDelMes(iso) {
  const [anio, mes] = iso.split('-').map(Number);
  return aIso(new Date(Date.UTC(anio, mes, 0)));
}

export function sumarMeses(iso, meses) {
  const [anio, mes] = iso.split('-').map(Number);
  return aIso(new Date(Date.UTC(anio, mes - 1 + meses, 1)));
}

const formatoMes = new Intl.DateTimeFormat('es', { month: 'long', year: 'numeric', timeZone: 'UTC' });
const formatoDia = new Intl.DateTimeFormat('es', { weekday: 'long', day: 'numeric', month: 'long', timeZone: 'UTC' });

export const nombreMes = (iso) => formatoMes.format(aUtc(iso));
export const fechaLarga = (iso) => formatoDia.format(aUtc(iso));

// Instante UTC del servidor. Algunos llegan sin «Z» (EF devuelve DateTime sin Kind): se toman como UTC.
export function instanteUtc(texto) {
  if (!texto) return null;
  return new Date(/[zZ]|[+-]\d\d:\d\d$/.test(texto) ? texto : `${texto}Z`);
}

// Día local (del navegador) de un instante, como AAAA-MM-DD.
export function diaLocalDe(fecha) {
  const anio = fecha.getFullYear();
  const mes = String(fecha.getMonth() + 1).padStart(2, '0');
  const dia = String(fecha.getDate()).padStart(2, '0');
  return `${anio}-${mes}-${dia}`;
}

// Días programados como texto corto: «L · X · V», «Todos los días» o «Entre semana».
export function describirDias(dias) {
  const ordenados = [...new Set(dias)].sort((a, b) => a - b);
  if (ordenados.length === 7) return 'Todos los días';
  if (ordenados.join() === ENTRE_SEMANA.join()) return 'Entre semana';
  if (ordenados.join() === '6,7') return 'Fines de semana';
  return ordenados.map((d) => DIAS_SEMANA[d - 1].corto).join(' · ');
}
