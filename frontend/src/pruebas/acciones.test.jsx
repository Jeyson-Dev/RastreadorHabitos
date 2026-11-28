import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BookOpen, Droplets } from 'lucide-react';
import { colorDeHabito, iconoDeHabito, inicialDe } from '../utilidades/identidadHabito';
import { abrir, conSesionGuardada, llamadasA, simularApi, usuarioDePrueba } from './montaje';

const TODOS = [1, 2, 3, 4, 5, 6, 7];

function habito(extra = {}) {
  return {
    id: 'h1',
    nombre: 'Leer',
    descripcion: null,
    dias: TODOS,
    fechaCreacionUtc: '2026-10-01T14:00:00Z',
    archivado: false,
    fechaArchivadoUtc: null,
    rachaActual: 2,
    mejorRacha: 5,
    programadoHoy: true,
    cumplidoHoy: null,
    ...extra,
  };
}

const historial = { habitoId: 'h1', desde: '2026-10-25', hasta: '2026-11-23', rachaActual: 2, mejorRacha: 5, registros: [] };

function meta(extra = {}) {
  return {
    id: 'm1',
    nombre: 'Leer 20 veces',
    objetivoCumplimientos: 20,
    avance: 3,
    fechaInicio: '2026-11-01',
    fechaLimite: '2026-12-15',
    estado: 'EnProgreso',
    habitoIds: ['h1'],
    fechaCreacionUtc: '2026-11-01T14:00:00Z',
    ...extra,
  };
}

beforeEach(() => conSesionGuardada());
afterEach(() => vi.unstubAllGlobals());

describe('identidad automática de cada hábito', () => {
  it('deduce el ícono del nombre, sin tildes ni mayúsculas, y si no reconoce nada usa la inicial', () => {
    expect(iconoDeHabito('Leer 20 minutos')).toBe(BookOpen);
    expect(iconoDeHabito('Tomar AGUA')).toBe(Droplets);
    expect(iconoDeHabito('Xilófono raro')).toBeNull();
    expect(inicialDe('  meditar')).toBe('M');
  });

  it('da siempre el mismo color al mismo hábito', () => {
    expect(colorDeHabito('h1')).toEqual(colorDeHabito('h1'));
    expect(colorDeHabito('h1').fondo).toMatch(/^var\(--h[1-8]-fondo\)$/);
  });
});

describe('acciones de un hábito', () => {
  it('edita un hábito desde el menú «⋯» y envía los datos nuevos', async () => {
    // Preparar
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, [habito()]],
      '/api/habitos/h1/registros': [200, historial],
      'PUT /api/habitos/h1': [200, habito({ nombre: 'Leer 30 minutos' })],
    });
    abrir('/habitos');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByRole('button', { name: 'Acciones de Leer' }));
    await persona.click(await screen.findByRole('menuitem', { name: 'Editar' }));
    const nombre = await screen.findByLabelText('Nombre');
    expect(nombre).toHaveValue('Leer');
    await persona.clear(nombre);
    await persona.type(nombre, 'Leer 30 minutos');
    await persona.click(screen.getByRole('button', { name: 'Guardar cambios' }));

    // Verificar
    expect(await screen.findByText('Guardaste los cambios de «Leer 30 minutos».')).toBeInTheDocument();
    expect(llamadasA(api, 'PUT', '/api/habitos/h1')[0].cuerpo).toEqual({ nombre: 'Leer 30 minutos', descripcion: null, dias: TODOS });
  });

  it('archiva un hábito: pasa a la sección de archivados y deja de contar para el límite', async () => {
    // Preparar
    let archivado = false;
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': () => [200, [habito({ archivado, fechaArchivadoUtc: archivado ? '2026-11-23T15:00:00Z' : null }), habito({ id: 'h2', nombre: 'Correr' })]],
      'POST /api/habitos/h1/archivar': () => {
        archivado = true;
        return [200, habito({ archivado: true })];
      },
    });
    abrir('/habitos');
    const persona = userEvent.setup();
    expect(await screen.findByText('2 de 10')).toBeInTheDocument();

    // Actuar
    await persona.click(screen.getByRole('button', { name: 'Acciones de Leer' }));
    await persona.click(await screen.findByRole('menuitem', { name: 'Archivar' }));

    // Verificar
    expect(await screen.findByText(/Archivaste «Leer»/)).toBeInTheDocument();
    expect(await screen.findByText('1 de 10')).toBeInTheDocument();
    expect(screen.getByText(/Archivados/)).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/habitos/h1/archivar')).toHaveLength(1);
  });

  it('restaurar con el límite lleno muestra el mensaje del servidor', async () => {
    // Preparar
    simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, [habito({ archivado: true, fechaArchivadoUtc: '2026-11-20T15:00:00Z' })]],
      'POST /api/habitos/h1/restaurar': [403, { error: 'Alcanzó el límite de 10 hábitos.' }],
    });
    abrir('/habitos');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByText(/Archivados/));
    await persona.click(screen.getByRole('button', { name: 'Acciones de Leer' }));
    await persona.click(await screen.findByRole('menuitem', { name: 'Restaurar' }));

    // Verificar
    expect(await screen.findByText('Alcanzó el límite de 10 hábitos.')).toBeInTheDocument();
  });

  it('eliminar pide confirmación, avisa cuántas metas lo pierden y luego lo borra', async () => {
    // Preparar
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, [habito()]],
      '/api/metas': [200, [meta()]],
      'DELETE /api/habitos/h1': [204, null],
    });
    abrir('/habitos');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByRole('button', { name: 'Acciones de Leer' }));
    await persona.click(await screen.findByRole('menuitem', { name: 'Eliminar' }));
    const dialogo = await screen.findByRole('dialog', { name: '¿Eliminar «Leer»?' });
    await waitFor(() => expect(dialogo).toHaveAccessibleDescription(expect.stringContaining('Una meta deja de contarlo.')));
    expect(llamadasA(api, 'DELETE', '/api/habitos/h1')).toHaveLength(0);
    await persona.click(within(dialogo).getByRole('button', { name: 'Eliminar hábito' }));

    // Verificar
    expect(await screen.findByText('Eliminaste «Leer».')).toBeInTheDocument();
    expect(llamadasA(api, 'DELETE', '/api/habitos/h1')).toHaveLength(1);
  });

  it('el menú se recorre con flechas y Escape lo cierra devolviendo el foco al botón', async () => {
    // Preparar
    simularApi({ '/api/sesion/usuario': [200, usuarioDePrueba()], '/api/habitos': [200, [habito()]] });
    abrir('/habitos');
    const persona = userEvent.setup();
    const boton = await screen.findByRole('button', { name: 'Acciones de Leer' });

    // Actuar
    await persona.click(boton);
    const editar = await screen.findByRole('menuitem', { name: 'Editar' });
    await waitFor(() => expect(editar).toHaveFocus());
    await persona.keyboard('{ArrowDown}');
    expect(screen.getByRole('menuitem', { name: 'Archivar' })).toHaveFocus();
    await persona.keyboard('{Escape}');

    // Verificar
    expect(boton).toHaveFocus();
    expect(boton).toHaveAttribute('aria-expanded', 'false');
  });
});

describe('eliminar una meta', () => {
  it('pide confirmación y la borra', async () => {
    // Preparar
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, [habito()]],
      '/api/metas': [200, [meta()]],
      'DELETE /api/metas/m1': [204, null],
    });
    abrir('/metas');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByRole('button', { name: 'Acciones de Leer 20 veces' }));
    await persona.click(await screen.findByRole('menuitem', { name: 'Eliminar' }));
    const dialogo = await screen.findByRole('dialog', { name: '¿Eliminar la meta «Leer 20 veces»?' });
    await persona.click(within(dialogo).getByRole('button', { name: 'Eliminar meta' }));

    // Verificar
    expect(await screen.findByText('Eliminaste la meta «Leer 20 veces».')).toBeInTheDocument();
    expect(llamadasA(api, 'DELETE', '/api/metas/m1')).toHaveLength(1);
  });
});
