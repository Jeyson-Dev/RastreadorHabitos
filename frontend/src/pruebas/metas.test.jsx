import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { diaLocalDe, sumarDias } from '../utilidades/fechas';
import { abrir, conSesionGuardada, llamadasA, simularApi, usuarioDePrueba } from './montaje';

const HOY = diaLocalDe(new Date());

function meta(extra = {}) {
  return {
    id: 'm1',
    nombre: 'Leer 20 veces',
    objetivoCumplimientos: 20,
    avance: 14,
    fechaInicio: sumarDias(HOY, -10),
    fechaLimite: sumarDias(HOY, 22),
    estado: 'EnProgreso',
    habitoIds: ['h1'],
    fechaCreacionUtc: '2026-11-13T12:00:00',
    ...extra,
  };
}

function habito(id, nombre) {
  return { id, nombre, descripcion: null, dias: [1, 2, 3, 4, 5, 6, 7], fechaCreacionUtc: '2026-10-01T12:00:00', rachaActual: 0, mejorRacha: 0, programadoHoy: true, cumplidoHoy: null };
}

function apiBase(extra = {}) {
  return simularApi({
    '/api/sesion/usuario': [200, usuarioDePrueba()],
    '/api/habitos': [200, [habito('h1', 'Leer'), habito('h2', 'Correr')]],
    ...extra,
  });
}

beforeEach(() => conSesionGuardada());
afterEach(() => vi.unstubAllGlobals());

describe('lista de metas por estado', () => {
  it('agrupa en curso, fallidas y completadas, y muestra solo la acción posible', async () => {
    // Preparar
    apiBase({
      '/api/metas': [
        200,
        [
          meta(),
          meta({ id: 'm2', nombre: 'Meditar 10', estado: 'Pendiente', avance: 0, objetivoCumplimientos: 10 }),
          meta({ id: 'm3', nombre: 'Correr 10', estado: 'Fallida', avance: 3, objetivoCumplimientos: 10, fechaLimite: sumarDias(HOY, -1) }),
          meta({ id: 'm4', nombre: 'Agua 30', estado: 'Completada', avance: 30, objetivoCumplimientos: 30 }),
        ],
      ],
    });

    // Actuar
    abrir('/metas');

    // Verificar
    const enCurso = await screen.findByRole('region', { name: 'En curso' });
    expect(within(enCurso).getByText('Leer 20 veces')).toBeInTheDocument();
    expect(within(enCurso).getByText('Faltan 6 cumplimientos')).toBeInTheDocument();
    expect(within(enCurso).getByText(/Aún sin cumplimientos/)).toBeInTheDocument();
    expect(within(enCurso).queryByRole('button', { name: /Completar/ })).not.toBeInTheDocument();
    const fallidas = screen.getByRole('region', { name: 'Fallidas' });
    expect(within(fallidas).getByRole('button', { name: 'Reactivar' })).toBeInTheDocument();
    expect(within(fallidas).getByText(/Venció el/)).toBeInTheDocument();
    expect(screen.getByText('Agua 30').closest('details')).not.toHaveAttribute('open');
  });

  it('completa una meta en progreso con el objetivo alcanzado, tras confirmar', async () => {
    // Preparar
    const api = apiBase({
      '/api/metas': [200, [meta({ avance: 20 })]],
      'POST /api/metas/m1/completar': [200, meta({ avance: 20, estado: 'Completada' })],
    });
    abrir('/metas');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByRole('button', { name: 'Completar meta' }));
    await persona.click(screen.getByRole('button', { name: 'Sí, completar' }));

    // Verificar
    expect(llamadasA(api, 'POST', '/api/metas/m1/completar')).toHaveLength(1);
  });

  it('muestra el rechazo del servidor al completar (409)', async () => {
    // Preparar
    apiBase({
      '/api/metas': [200, [meta({ avance: 20 })]],
      'POST /api/metas/m1/completar': [409, { error: 'La meta no puede completarse: el avance no alcanza el objetivo.' }],
    });
    abrir('/metas');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByRole('button', { name: 'Completar meta' }));
    await persona.click(screen.getByRole('button', { name: 'Sí, completar' }));

    // Verificar
    expect(await screen.findByRole('alert')).toHaveTextContent('el avance no alcanza el objetivo');
  });

  it('reactiva una meta fallida con una fecha límite nueva y rechaza una vacía', async () => {
    // Preparar
    const nueva = sumarDias(HOY, 30);
    const api = apiBase({
      '/api/metas': [200, [meta({ estado: 'Fallida', fechaLimite: sumarDias(HOY, -1) })]],
      'POST /api/metas/m1/reactivar': [200, meta({ estado: 'EnProgreso', fechaLimite: nueva })],
    });
    abrir('/metas');
    const persona = userEvent.setup();

    // Actuar: fecha vacía
    await persona.click(await screen.findByRole('button', { name: 'Reactivar' }));
    const campo = screen.getByLabelText('Nueva fecha límite');
    fireEvent.change(campo, { target: { value: '' } });
    await persona.click(screen.getByRole('button', { name: 'Reactivar meta' }));

    // Verificar
    expect(screen.getByText('Elige la nueva fecha límite.')).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/metas/m1/reactivar')).toHaveLength(0);

    // Actuar: fecha válida
    fireEvent.change(campo, { target: { value: nueva } });
    await persona.click(screen.getByRole('button', { name: 'Reactivar meta' }));

    // Verificar
    expect(llamadasA(api, 'POST', '/api/metas/m1/reactivar')[0].cuerpo).toEqual({ nuevaFechaLimite: nueva });
  });
});

describe('nueva meta', () => {
  it.each([
    ['vacíos', '', ''],
    ['en blanco', '   ', '  '],
    ['inválidos', 'Leer', '0'],
  ])('con datos %s no envía nada', async (_caso, nombre, objetivo) => {
    // Preparar
    const api = apiBase({ '/api/metas': [200, []] });
    abrir('/metas?nueva=1');
    const persona = userEvent.setup();

    // Actuar
    const campoNombre = await screen.findByLabelText('Nombre');
    if (nombre) await persona.type(campoNombre, nombre);
    if (objetivo) await persona.type(screen.getByLabelText('Cumplimientos objetivo'), objetivo);
    await persona.click(screen.getByRole('button', { name: 'Crear meta' }));

    // Verificar
    expect(screen.getByLabelText('Cumplimientos objetivo')).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByText('Elige al menos un hábito.')).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/metas')).toHaveLength(0);
  });

  it('crea la meta con su objetivo, fecha y hábitos', async () => {
    // Preparar
    const limite = sumarDias(HOY, 20);
    const api = apiBase({ '/api/metas': [200, []], 'POST /api/metas': [201, meta({ estado: 'Pendiente', avance: 0 })] });
    abrir('/metas?nueva=1');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Nombre'), 'Leer 20 veces');
    await persona.type(screen.getByLabelText('Cumplimientos objetivo'), '20');
    fireEvent.change(screen.getByLabelText('Fecha límite'), { target: { value: limite } });
    await persona.click(screen.getByRole('checkbox', { name: /Leer/ }));
    await persona.click(screen.getByRole('button', { name: 'Crear meta' }));

    // Verificar
    expect(await screen.findByText(/Creaste la meta «Leer 20 veces»/)).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/metas')[0].cuerpo).toEqual({
      nombre: 'Leer 20 veces',
      objetivoCumplimientos: 20,
      fechaLimite: limite,
      habitoIds: ['h1'],
    });
  });
});

describe('límite de hábitos y permiso ampliado', () => {
  const nueve = Array.from({ length: 9 }, (_, i) => habito(`h${i}`, `Hábito ${i}`));

  it('cerca del límite ofrece pedir el permiso y lo solicita', async () => {
    // Preparar
    let pendientes = [];
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, nueve],
      '/api/solicitudes-permiso/pendientes': () => [200, pendientes],
      'POST /api/solicitudes-permiso': () => {
        pendientes = [{ id: 's1', permiso: 'LIMITE_HABITOS_AMPLIADO', estado: 'Pendiente' }];
        return [201, pendientes[0]];
      },
    });
    abrir('/habitos');
    const persona = userEvent.setup();

    // Actuar
    expect(await screen.findByText('9 de 10')).toBeInTheDocument();
    await persona.click(await screen.findByRole('button', { name: 'Pedir límite de 20' }));

    // Verificar
    expect(await screen.findByText(/Tu solicitud del límite de 20 hábitos está pendiente/)).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/solicitudes-permiso')[0].cuerpo).toEqual({ permiso: 'LIMITE_HABITOS_AMPLIADO' });
  });

  it('con una solicitud ya pendiente no deja pedirla otra vez', async () => {
    // Preparar
    simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, nueve],
      '/api/solicitudes-permiso/pendientes': [200, [{ id: 's1', permiso: 'LIMITE_HABITOS_AMPLIADO', estado: 'Pendiente' }]],
    });

    // Actuar
    abrir('/habitos');

    // Verificar
    expect(await screen.findByText(/está pendiente/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Pedir límite de 20' })).not.toBeInTheDocument();
  });

  it('con el permiso ampliado el límite es 20 y no hay aviso', async () => {
    // Preparar
    simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba('Estandar', { permisos: ['LIMITE_HABITOS_AMPLIADO'] })],
      '/api/habitos': [200, nueve],
    });

    // Actuar
    abrir('/habitos');

    // Verificar
    expect(await screen.findByText('9 de 20')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Pedir límite de 20' })).not.toBeInTheDocument();
  });
});
