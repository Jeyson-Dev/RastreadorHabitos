import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import PaginaError from '../paginas/PaginaError';
import { abrir, simularApi } from './montaje';

afterEach(() => {
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

function Explota() {
  throw new Error('TypeError: cannot read properties of undefined (detalle interno)');
}

describe('pulido', () => {
  it('un error al mostrar una pantalla da un mensaje comprensible, sin el detalle técnico', () => {
    // Preparar
    vi.spyOn(console, 'error').mockImplementation(() => {});
    const enrutador = createMemoryRouter([{ path: '/', element: <Explota />, errorElement: <PaginaError /> }]);

    // Actuar
    render(<RouterProvider router={enrutador} />);

    // Verificar
    expect(screen.getByRole('heading', { name: 'Algo salió mal al mostrar esta pantalla' })).toBeInTheDocument();
    expect(screen.queryByText(/detalle interno/)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Recargar' })).toBeInTheDocument();
  });

  it('las pantallas de acceso ponen su título en la pestaña', async () => {
    // Preparar
    simularApi({});

    // Actuar
    abrir('/registro');

    // Verificar
    await screen.findByRole('heading', { name: 'Crea tu cuenta' });
    expect(document.title).toBe('Crear cuenta · Rastreador de hábitos');
  });
});
