import { pedir } from './cliente';

// Hábitos, registro diario, historial con rachas y plan de hoy. La fecha de «hoy» siempre la pone
// el servidor (día local de su zona horaria).

export const CLAVES_HABITOS = {
  lista: ['habitos'],
  registros: (id, desde, hasta) => ['habitos', id, 'registros', desde ?? null, hasta ?? null],
  planDeHoy: (ciudad) => ['habitos', 'plan-de-hoy', ciudad],
};

export const listarHabitos = (senal) => pedir('/api/habitos', { senal });

export const crearHabito = ({ nombre, descripcion, dias }) =>
  pedir('/api/habitos', { metodo: 'POST', cuerpo: { nombre, descripcion: descripcion || null, dias } });

export const registrarCumplimiento = (id, cumplido) =>
  pedir(`/api/habitos/${encodeURIComponent(id)}/registros`, { metodo: 'POST', cuerpo: { cumplido } });

export const consultarRegistros = (id, { desde, hasta } = {}, senal) =>
  pedir(`/api/habitos/${encodeURIComponent(id)}/registros`, { consulta: { desde, hasta }, senal });

export const consultarPlanDeHoy = (ciudad, senal) => pedir('/api/habitos/hoy', { consulta: { ciudad }, senal });
