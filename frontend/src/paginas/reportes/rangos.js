import { diaLocalDe, sumarDias, sumarMeses } from '../../utilidades/fechas';

// Rangos predefinidos del selector de fechas (días locales AAAA-MM-DD; el servidor los interpreta en su zona).
export const RANGOS = [
  { id: '30d', texto: '30 días', desde: (hoy) => sumarDias(hoy, -29) },
  { id: '3m', texto: '3 meses', desde: (hoy) => sumarMeses(`${hoy.slice(0, 8)}01`, -2) },
  { id: '6m', texto: '6 meses', desde: (hoy) => sumarMeses(`${hoy.slice(0, 8)}01`, -5) },
  { id: '12m', texto: '12 meses', desde: (hoy) => sumarMeses(`${hoy.slice(0, 8)}01`, -11) },
];

export function rangoPredefinido(id, hoy = diaLocalDe(new Date())) {
  const rango = RANGOS.find((r) => r.id === id) ?? RANGOS[1];
  return { id: rango.id, desde: rango.desde(hoy), hasta: hoy };
}

// Etiquetas de los grupos que devuelve la API: «2026-11» (mes) o «2026-11-16» (lunes de la semana).
const formatoMes = new Intl.DateTimeFormat('es', { month: 'short', year: '2-digit', timeZone: 'UTC' });
const formatoDia = new Intl.DateTimeFormat('es', { day: 'numeric', month: 'short', timeZone: 'UTC' });

export function etiquetaGrupo(grupo) {
  const [anio, mes, dia] = grupo.split('-').map(Number);
  const fecha = new Date(Date.UTC(anio, mes - 1, dia ?? 1));
  return (dia ? formatoDia : formatoMes).format(fecha).replace('.', '');
}

// Estados de una solicitud en orden fijo de la paleta categórica (validada en claro y oscuro con la
// skill dataviz). El color sigue al estado, nunca a su posición.
export const SERIES_SOLICITUDES = [
  { clave: 'Pendiente', texto: 'Pendientes', color: 'var(--serie-1)' },
  { clave: 'Aprobada', texto: 'Aprobadas', color: 'var(--serie-2)' },
  { clave: 'Aplicada', texto: 'Aplicadas', color: 'var(--serie-3)' },
  { clave: 'Rechazada', texto: 'Rechazadas', color: 'var(--serie-4)' },
];
