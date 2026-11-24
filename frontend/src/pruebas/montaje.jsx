// Ayudas comunes de las pruebas: una API simulada por ruta y la app montada con sus proveedores
// sobre un enrutador en memoria. Ninguna prueba levanta el servidor.
import { vi } from 'vitest';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider } from 'react-router';
import ProveedorSesion from '../sesion/ProveedorSesion';
import ProveedorTema from '../tema/ProveedorTema';
import { crearRutas } from '../rutas';

export const EN_OCHO_HORAS = () => new Date(Date.now() + 8 * 3600 * 1000).toISOString();

export function json(estado, cuerpo) {
  return new Response(JSON.stringify(cuerpo), { status: estado, headers: { 'Content-Type': 'application/json' } });
}

export function usuarioDePrueba(rol = 'Estandar', extra = {}) {
  return { id: 'u1', email: 'ana@correo.com', nombreCompleto: 'Ana Pérez', rol, permisos: [], ...extra };
}

// rutas: { 'POST /api/x': [estado, cuerpo] | (opciones) => [estado, cuerpo] }; sin método = GET.
// Una petición no prevista hace fallar la prueba.
export function simularApi(rutas) {
  const falso = vi.fn(async (url, opciones = {}) => {
    const ruta = String(url).split('?')[0];
    const metodo = opciones.method ?? 'GET';
    const respuesta = rutas[`${metodo} ${ruta}`] ?? (metodo === 'GET' ? rutas[ruta] : undefined);
    if (!respuesta) throw new Error(`Petición no prevista: ${metodo} ${ruta}`);
    const [estado, cuerpo] = typeof respuesta === 'function' ? respuesta(opciones, String(url)) : respuesta;
    return json(estado, cuerpo);
  });
  vi.stubGlobal('fetch', falso);
  return falso;
}

// Llamadas hechas a una ruta (método y cuerpo ya interpretado).
export function llamadasA(falso, metodo, ruta) {
  return falso.mock.calls
    .filter(([url, opciones = {}]) => String(url).split('?')[0] === ruta && (opciones.method ?? 'GET') === metodo)
    .map(([url, opciones = {}]) => ({
      url: String(url),
      cuerpo: typeof opciones.body === 'string' ? JSON.parse(opciones.body) : opciones.body,
      opciones,
    }));
}

export function conSesionGuardada(expiraUtc = EN_OCHO_HORAS()) {
  localStorage.setItem('rh.sesion', JSON.stringify({ token: 'token-de-prueba', expiraUtc }));
}

export function abrir(ruta) {
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
