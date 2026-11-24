import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describirDias, diaSemana, instanteUtc, rangoDias, sumarMeses, ultimoDiaDelMes } from '../utilidades/fechas';
import { estadoDelDia } from '../paginas/habitos/estadoDia';
import { abrir, conSesionGuardada, llamadasA, simularApi, usuarioDePrueba } from './montaje';

const TODOS = [1, 2, 3, 4, 5, 6, 7];

function habito(extra = {}) {
  return {
    id: 'h1',
    nombre: 'Leer',
    descripcion: null,
    dias: TODOS,
    fechaCreacionUtc: '2026-10-01T14:00:00',
    rachaActual: 2,
    mejorRacha: 5,
    programadoHoy: true,
    cumplidoHoy: null,
    ...extra,
  };
}

const plan = {
  fecha: '2026-11-23',
  ciudad: 'Santo Domingo',
  pais: 'República Dominicana',
  pronostico: { condicion: 'Soleado', temperaturaMaximaC: 30.4, temperaturaMinimaC: 22.1, probabilidadLluvia: 10 },
  favorableAlAireLibre: true,
  recomendacion: 'Buen momento para salir.',
  pendientes: 1,
  habitos: [],
};

beforeEach(() => conSesionGuardada());
afterEach(() => vi.unstubAllGlobals());

describe('fechas del dominio', () => {
  it('calcula el día de la semana de 1 (lunes) a 7 (domingo) sin depender de la zona del navegador', () => {
    expect(diaSemana('2026-11-23')).toBe(1);
    expect(diaSemana('2026-11-29')).toBe(7);
  });

  it('arma rangos de días y meses, incluido el cambio de año y febrero bisiesto', () => {
    expect(rangoDias('2026-12-30', '2027-01-02')).toEqual(['2026-12-30', '2026-12-31', '2027-01-01', '2027-01-02']);
    expect(ultimoDiaDelMes('2028-02-10')).toBe('2028-02-29');
    expect(sumarMeses('2026-01-01', -1)).toBe('2025-12-01');
  });

  it('describe los días programados', () => {
    expect(describirDias([1, 3, 5])).toBe('L · X · V');
    expect(describirDias(TODOS)).toBe('Todos los días');
    expect(describirDias([5, 4, 3, 2, 1])).toBe('Entre semana');
  });

  it('toma como UTC un instante que llega sin «Z»', () => {
    expect(instanteUtc('2026-11-24T02:32:40.4055585').toISOString()).toBe('2026-11-24T02:32:40.405Z');
    expect(instanteUtc('2026-11-24T02:32:40Z').toISOString()).toBe('2026-11-24T02:32:40.000Z');
  });

  it('clasifica cada día del historial', () => {
    const contexto = { habito: habito({ dias: [1, 3, 5] }), registros: new Map([['2026-11-16', true], ['2026-11-18', false]]), hoy: '2026-11-23', creacion: '2026-11-10' };
    expect(estadoDelDia('2026-11-16', contexto)).toBe('cumplido');
    expect(estadoDelDia('2026-11-18', contexto)).toBe('no-cumplido');
    expect(estadoDelDia('2026-11-20', contexto)).toBe('sin-registro');
    expect(estadoDelDia('2026-11-21', contexto)).toBe('no-programado');
    expect(estadoDelDia('2026-11-23', contexto)).toBe('pendiente');
    expect(estadoDelDia('2026-11-09', contexto)).toBe('fuera');
  });
});

describe('pantalla Hoy', () => {
  it('muestra el progreso y registra «Hecho», que se refleja al recargar la lista', async () => {
    // Preparar
    let cumplido = null;
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': () => [200, [habito({ cumplidoHoy: cumplido }), habito({ id: 'h2', nombre: 'Correr', cumplidoHoy: true })]],
      '/api/habitos/hoy': [200, plan],
      'POST /api/habitos/h1/registros': () => {
        cumplido = true;
        return [200, { habitoId: 'h1', fecha: '2026-11-23', cumplido: true }];
      },
    });
    abrir('/');
    const persona = userEvent.setup();

    // Actuar
    const grupo = await screen.findByRole('group', { name: 'Registrar hoy: Leer' });
    expect(screen.getByRole('progressbar', { name: 'Hábitos cumplidos hoy' })).toHaveAttribute('aria-valuenow', '1');
    await persona.click(within(grupo).getByRole('button', { name: 'Hecho' }));

    // Verificar
    expect(await screen.findByRole('button', { name: 'Cambiar el registro de hoy de Leer' })).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/habitos/h1/registros')[0].cuerpo).toEqual({ cumplido: true });
    expect(screen.getByRole('progressbar', { name: 'Hábitos cumplidos hoy' })).toHaveAttribute('aria-valuenow', '2');
  });

  it('«No lo hice» envía cumplido: false', async () => {
    // Preparar
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, [habito()]],
      '/api/habitos/hoy': [200, plan],
      'POST /api/habitos/h1/registros': [200, { habitoId: 'h1', fecha: '2026-11-23', cumplido: false }],
    });
    abrir('/');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByRole('button', { name: 'No lo hice' }));

    // Verificar
    expect(llamadasA(api, 'POST', '/api/habitos/h1/registros')[0].cuerpo).toEqual({ cumplido: false });
  });

  it('si el clima no está disponible (503) lo indica y los hábitos siguen funcionando', async () => {
    // Preparar
    simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, [habito()]],
      '/api/habitos/hoy': [503, { error: 'El plan de hoy no está disponible: el servicio de clima rechazó la clave configurada en el servidor.' }],
    });

    // Actuar
    abrir('/');

    // Verificar
    expect(await screen.findByText('El clima no está disponible ahora')).toBeInTheDocument();
    expect(screen.getByRole('group', { name: 'Registrar hoy: Leer' })).toBeInTheDocument();
  });

  it('sin hábitos invita a crear el primero', async () => {
    // Preparar
    simularApi({ '/api/sesion/usuario': [200, usuarioDePrueba()], '/api/habitos': [200, []], '/api/habitos/hoy': [200, plan] });

    // Actuar
    abrir('/');

    // Verificar
    expect(await screen.findByText('Aún no tienes hábitos')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Crear un hábito' })).toHaveAttribute('href', '/habitos?nuevo=1');
  });
});

describe('pantalla Hábitos', () => {
  it('resume los últimos 30 días de cada hábito a partir de su historial', async () => {
    // Preparar
    simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, [habito()]],
      '/api/habitos/h1/registros': [
        200,
        {
          habitoId: 'h1',
          desde: '2026-10-25',
          hasta: '2026-11-23',
          rachaActual: 2,
          mejorRacha: 5,
          registros: [
            { habitoId: 'h1', fecha: '2026-11-21', cumplido: true },
            { habitoId: 'h1', fecha: '2026-11-22', cumplido: true },
            { habitoId: 'h1', fecha: '2026-11-20', cumplido: false },
          ],
        },
      ],
    });

    // Actuar
    abrir('/habitos');

    // Verificar: 30 días programados, 2 cumplidos, 1 no cumplido, hoy pendiente y 26 sin registrar.
    expect(
      await screen.findByRole('button', { description: 'Últimos 30 días: 2 cumplidos, 1 no cumplidos y 26 sin registrar.' }),
    ).toHaveAccessibleName(expect.stringMatching(/^Leer.*Todos los días.*racha/));
  });

  it.each([
    ['vacío', ''],
    ['en blanco', '    '],
  ])('no crea un hábito con nombre %s ni sin días', async (_caso, nombre) => {
    // Preparar
    const api = simularApi({ '/api/sesion/usuario': [200, usuarioDePrueba()], '/api/habitos': [200, [habito()]], '/api/habitos/h1/registros': [500, {}] });
    abrir('/habitos?nuevo=1');
    const persona = userEvent.setup();

    // Actuar
    const nombreCampo = await screen.findByLabelText('Nombre');
    if (nombre) await persona.type(nombreCampo, nombre);
    await persona.click(screen.getByRole('button', { name: 'Crear hábito' }));

    // Verificar
    expect(nombreCampo).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByText('Elige al menos un día de la semana.')).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/habitos')).toHaveLength(0);
  });

  it('crea un hábito con los días elegidos', async () => {
    // Preparar
    const api = simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, []],
      'POST /api/habitos': [201, habito({ id: 'h9', nombre: 'Meditar', dias: [1, 2, 3, 4, 5] })],
    });
    abrir('/habitos?nuevo=1');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Nombre'), '  Meditar ');
    await persona.click(screen.getByRole('button', { name: 'Entre semana' }));
    await persona.click(screen.getByRole('button', { name: 'Crear hábito' }));

    // Verificar
    expect(await screen.findByText('Creaste «Meditar».')).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/habitos')[0].cuerpo).toEqual({ nombre: 'Meditar', descripcion: null, dias: [1, 2, 3, 4, 5] });
  });

  it('al llegar al límite (403) muestra el mensaje del servidor', async () => {
    // Preparar
    simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      '/api/habitos': [200, []],
      'POST /api/habitos': [403, { error: 'Alcanzó el límite de 10 hábitos. Para crear más, solicite el permiso LIMITE_HABITOS_AMPLIADO.' }],
    });
    abrir('/habitos?nuevo=1');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Nombre'), 'Uno más');
    await persona.click(screen.getByRole('button', { name: 'lunes' }));
    await persona.click(screen.getByRole('button', { name: 'Crear hábito' }));

    // Verificar
    expect(await screen.findByText('Llegaste al límite de hábitos')).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('Alcanzó el límite de 10 hábitos.');
  });
});
