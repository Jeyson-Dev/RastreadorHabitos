import { pedir } from './cliente';

// Reportes con agregación: la API filtra por rol (un Estándar ve sus datos; el Administrador, todos).
export const REPORTES = {
  solicitudes: 'solicitudes-por-estado',
  cumplimiento: 'cumplimiento-semanal',
};

export const claveReporte = (reporte, desde, hasta) => ['reportes', reporte, desde, hasta];

export const consultarReporte = (reporte, { desde, hasta }, senal) =>
  pedir(`/api/reportes/${reporte}`, { consulta: { desde, hasta }, senal });
