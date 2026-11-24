import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { etiquetaGrupo, rangoPredefinido } from '../paginas/reportes/rangos';
import { haceCuanto } from '../utilidades/tiempo';
import { abrir, conSesionGuardada, llamadasA, simularApi, usuarioDePrueba } from './montaje';

const notificacion = (extra = {}) => ({
  id: 'n1',
  tipo: 'MetaCambioEstado',
  mensaje: 'Tu meta «Leer 20 veces» pasó de Pendiente a En progreso.',
  leida: false,
  fechaUtc: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
  fechaLecturaUtc: null,
  ...extra,
});

beforeEach(() => conSesionGuardada());
afterEach(() => vi.unstubAllGlobals());

describe('utilidades', () => {
  it('describe el tiempo transcurrido y acepta instantes sin «Z»', () => {
    const ahora = new Date('2026-11-24T12:00:00Z');
    expect(haceCuanto('2026-11-24T11:55:00', ahora)).toBe('hace 5 minutos');
    expect(haceCuanto('2026-11-23T12:00:00Z', ahora)).toBe('ayer');
  });

  it('calcula los rangos rápidos y las etiquetas de mes y semana', () => {
    expect(rangoPredefinido('3m', '2026-11-24')).toEqual({ id: '3m', desde: '2026-09-01', hasta: '2026-11-24' });
    expect(rangoPredefinido('30d', '2026-11-24').desde).toBe('2026-10-26');
    expect(etiquetaGrupo('2026-11')).toMatch(/nov/);
    expect(etiquetaGrupo('2026-11-16')).toMatch(/16/);
  });
});

describe('notificaciones', () => {
  it('muestra la bandeja, el contador de la campana y marca una como leída', async () => {
    // Preparar
    let leida = false;
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/notificaciones': (_opciones, url) => {
        const lista = [notificacion({ leida }), notificacion({ id: 'n2', tipo: 'SolicitudPermisoAprobada', mensaje: 'Aprobaron tu permiso.', leida: false })];
        const visibles = url.includes('leida=false') ? lista.filter((n) => !n.leida) : lista;
        return [200, { noLeidas: lista.filter((n) => !n.leida).length, notificaciones: visibles }];
      },
      '/api/notificaciones/preferencias': [200, { correoActivo: true }],
      'POST /api/notificaciones/n1/leida': () => {
        leida = true;
        return [200, notificacion({ leida: true })];
      },
    });
    abrir('/notificaciones');
    const persona = userEvent.setup();

    // Actuar
    expect(await screen.findByRole('link', { name: 'Notificaciones, 2 sin leer' })).toBeInTheDocument();
    await persona.click(await screen.findByRole('button', { name: 'Marcar como leída: Tu meta cambió de estado' }));

    // Verificar
    expect(llamadasA(api, 'POST', '/api/notificaciones/n1/leida')).toHaveLength(1);
    expect(await screen.findByRole('link', { name: 'Notificaciones, 1 sin leer' })).toBeInTheDocument();
  });

  it('filtra solo las no leídas', async () => {
    // Preparar
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/notificaciones': [200, { noLeidas: 0, notificaciones: [] }],
      '/api/notificaciones/preferencias': [200, { correoActivo: true }],
    });
    abrir('/notificaciones');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByRole('button', { name: 'No leídas' }));

    // Verificar
    expect(await screen.findByText('No tienes notificaciones sin leer')).toBeInTheDocument();
    expect(llamadasA(api, 'GET', '/api/notificaciones').some((l) => l.url.includes('leida=false'))).toBe(true);
  });

  it('apaga el correo con el interruptor', async () => {
    // Preparar
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/notificaciones': [200, { noLeidas: 0, notificaciones: [] }],
      '/api/notificaciones/preferencias': [200, { correoActivo: true }],
      'PUT /api/notificaciones/preferencias': [200, { correoActivo: false }],
    });
    abrir('/notificaciones');
    const persona = userEvent.setup();

    // Actuar
    const interruptor = await screen.findByRole('switch', { name: 'Recibir también por correo' });
    expect(interruptor).toHaveAttribute('aria-checked', 'true');
    await persona.click(interruptor);

    // Verificar
    expect(llamadasA(api, 'PUT', '/api/notificaciones/preferencias')[0].cuerpo).toEqual({ correoActivo: false });
    expect(await screen.findByRole('switch', { name: 'Recibir también por correo' })).toHaveAttribute('aria-checked', 'false');
  });
});

describe('reportes', () => {
  const cumplimiento = {
    reporte: 'cumplimiento-semanal',
    pregunta: '¿Qué porcentaje de los días programados cumpliste cada semana?',
    alcance: 'Solo sus datos',
    filas: [{ grupo: '2026-11-16', valores: { diasProgramados: 10, diasCumplidos: 8, porcentajeCumplimiento: 80 } }],
  };
  const solicitudesVacio = { reporte: 'solicitudes-por-estado', pregunta: '¿Cuántas solicitudes por estado?', alcance: 'Solo sus datos', filas: [] };

  it('muestra cada reporte con su alcance y su tabla de datos', async () => {
    // Preparar
    simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/reportes/cumplimiento-semanal': [200, cumplimiento],
      '/api/reportes/solicitudes-por-estado': [200, solicitudesVacio],
    });

    // Actuar
    abrir('/reportes');

    // Verificar
    const tarjeta = await screen.findByRole('region', { name: 'Cumplimiento semanal' });
    expect(await within(tarjeta).findByText('Solo sus datos')).toBeInTheDocument();
    expect(within(tarjeta).getByRole('cell', { name: '80 %' })).toBeInTheDocument();
    expect(within(screen.getByRole('region', { name: 'Solicitudes de permiso por estado' })).getByText('No hay datos en este rango')).toBeInTheDocument();
  });

  it('no aplica un rango con «desde» posterior a «hasta»', async () => {
    // Preparar
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/reportes/cumplimiento-semanal': [200, cumplimiento],
      '/api/reportes/solicitudes-por-estado': [200, solicitudesVacio],
    });
    abrir('/reportes');
    const persona = userEvent.setup();
    await screen.findByRole('region', { name: 'Cumplimiento semanal' });
    const antes = api.mock.calls.length;

    // Actuar
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-12-01' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-11-01' } });
    await persona.click(screen.getByRole('button', { name: 'Aplicar' }));

    // Verificar
    expect(screen.getByRole('alert')).toHaveTextContent('no puede ser posterior');
    expect(api.mock.calls.slice(antes).some(([url]) => String(url).includes('2026-12-01'))).toBe(false);
  });

  it('muestra el rechazo del servidor (400) con opción de reintentar', async () => {
    // Preparar
    simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/reportes/cumplimiento-semanal': [400, { error: 'El rango de fechas no es válido.' }],
      '/api/reportes/solicitudes-por-estado': [200, solicitudesVacio],
    });

    // Actuar
    abrir('/reportes');

    // Verificar
    const tarjeta = await screen.findByRole('region', { name: 'Cumplimiento semanal' });
    expect(await within(tarjeta).findByText('El rango de fechas no es válido.')).toBeInTheDocument();
    expect(within(tarjeta).getByRole('button', { name: 'Reintentar' })).toBeInTheDocument();
  });
});
