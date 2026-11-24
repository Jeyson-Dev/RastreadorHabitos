import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider } from 'react-router';
import ProveedorSesion from '../sesion/ProveedorSesion';
import ProveedorTema from '../tema/ProveedorTema';
import { crearRutas } from '../rutas';

const EN_OCHO_HORAS = new Date(Date.now() + 8 * 3600 * 1000).toISOString();

function json(estado, cuerpo) {
  return new Response(JSON.stringify(cuerpo), { status: estado, headers: { 'Content-Type': 'application/json' } });
}

function usuario(rol) {
  return { id: 'u1', email: 'ana@correo.com', nombreCompleto: 'Ana Pérez', rol, permisos: [] };
}

// Responde según la ruta; las no previstas fallan para que la prueba lo note.
function simularApi(rutas) {
  const falso = vi.fn(async (url) => {
    const ruta = String(url).split('?')[0];
    if (!(ruta in rutas)) throw new Error(`Petición no prevista: ${ruta}`);
    const [estado, cuerpo] = rutas[ruta];
    return json(estado, cuerpo);
  });
  vi.stubGlobal('fetch', falso);
  return falso;
}

function abrir(ruta) {
  const consultas = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const enrutador = createMemoryRouter(crearRutas(), { initialEntries: [ruta] });
  render(
    <QueryClientProvider client={consultas}>
      <ProveedorTema>
        <ProveedorSesion>
          <RouterProvider router={enrutador} />
        </ProveedorSesion>
      </ProveedorTema>
    </QueryClientProvider>,
  );
  return enrutador;
}

function conSesionGuardada(expiraUtc = EN_OCHO_HORAS) {
  localStorage.setItem('rh.sesion', JSON.stringify({ token: 'token-de-prueba', expiraUtc }));
}

describe('rutas protegidas por sesión y rol', () => {
  beforeEach(() => vi.unstubAllGlobals());
  afterEach(() => vi.unstubAllGlobals());

  it('sin sesión, una ruta protegida lleva al inicio de sesión', async () => {
    // Preparar
    simularApi({});

    // Actuar
    abrir('/metas');

    // Verificar
    expect(await screen.findByRole('heading', { name: 'Inicia sesión' })).toBeInTheDocument();
  });

  it('una sesión guardada ya vencida se descarta', async () => {
    // Preparar
    conSesionGuardada(new Date(Date.now() - 1000).toISOString());
    simularApi({});

    // Actuar
    abrir('/');

    // Verificar
    expect(await screen.findByRole('heading', { name: 'Inicia sesión' })).toBeInTheDocument();
    expect(localStorage.getItem('rh.sesion')).toBeNull();
  });

  it('un Estándar no ve las secciones de administración', async () => {
    // Preparar
    conSesionGuardada();
    simularApi({ '/api/sesion/usuario': [200, usuario('Estandar')] });

    // Actuar
    abrir('/admin/usuarios');

    // Verificar
    expect(await screen.findByText('Esta sección es solo para administradores')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Auditoría' })).not.toBeInTheDocument();
  });

  it('un Administrador sí entra a las secciones de administración', async () => {
    // Preparar
    conSesionGuardada();
    simularApi({ '/api/sesion/usuario': [200, usuario('Administrador')] });

    // Actuar
    abrir('/admin/usuarios');

    // Verificar
    expect(await screen.findByRole('heading', { level: 1, name: 'Usuarios y roles' })).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: 'Auditoría' }).length).toBeGreaterThan(0);
  });

  it('al iniciar sesión vuelve a la ruta que se pidió y envía el token', async () => {
    // Preparar
    const fetchFalso = simularApi({
      '/api/sesion/iniciar': [200, { token: 'nuevo-token', expiraUtc: EN_OCHO_HORAS }],
      '/api/sesion/usuario': [200, usuario('Estandar')],
    });
    const enrutador = abrir('/metas');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Correo'), 'ana@correo.com');
    await persona.type(screen.getByLabelText('Contraseña'), 'Habitos2026');
    await persona.click(screen.getByRole('button', { name: 'Iniciar sesión' }));

    // Verificar
    expect(await screen.findByRole('heading', { level: 1, name: 'Metas' })).toBeInTheDocument();
    expect(enrutador.state.location.pathname).toBe('/metas');
    const llamadaUsuario = fetchFalso.mock.calls.find(([url]) => url === '/api/sesion/usuario');
    expect(llamadaUsuario[1].headers.Authorization).toBe('Bearer nuevo-token');
  });

  it('muestra el rechazo del servidor al iniciar sesión, sin entrar', async () => {
    // Preparar
    simularApi({ '/api/sesion/iniciar': [403, { error: 'La cuenta aún no está activada.' }] });
    abrir('/iniciar-sesion');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Correo'), 'ana@correo.com');
    await persona.type(screen.getByLabelText('Contraseña'), 'Habitos2026');
    await persona.click(screen.getByRole('button', { name: 'Iniciar sesión' }));

    // Verificar
    expect(await screen.findByRole('alert')).toHaveTextContent('La cuenta aún no está activada.');
  });

  it('si el servidor rechaza el token (401), cierra la sesión y lo avisa', async () => {
    // Preparar
    conSesionGuardada();
    simularApi({ '/api/sesion/usuario': [401, { error: 'Se requiere una sesión válida.' }] });

    // Actuar
    abrir('/habitos');

    // Verificar
    expect(await screen.findByRole('heading', { name: 'Inicia sesión' })).toBeInTheDocument();
    expect(screen.getByText('Se requiere una sesión válida.')).toBeInTheDocument();
    expect(localStorage.getItem('rh.sesion')).toBeNull();
  });
});
