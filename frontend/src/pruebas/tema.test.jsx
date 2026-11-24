import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ProveedorTema from '../tema/ProveedorTema';
import InterruptorTema from '../tema/InterruptorTema';

function simularSistema(oscuro) {
  window.matchMedia = vi.fn().mockImplementation((consulta) => ({
    matches: oscuro,
    media: consulta,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  }));
}

function montar() {
  render(
    <ProveedorTema>
      <InterruptorTema />
    </ProveedorTema>,
  );
}

describe('tema claro y oscuro', () => {
  afterEach(() => document.documentElement.classList.remove('dark'));

  it('respeta la preferencia oscura del sistema si no se eligió otra', () => {
    // Preparar
    simularSistema(true);

    // Actuar
    montar();

    // Verificar
    expect(document.documentElement).toHaveClass('dark');
    expect(screen.getByRole('button', { name: /tema del sistema/i })).toBeInTheDocument();
  });

  it('el interruptor recorre claro y oscuro y recuerda la elección', async () => {
    // Preparar
    simularSistema(true);
    montar();
    const usuario = userEvent.setup();

    // Actuar: sistema → claro
    await usuario.click(screen.getByRole('button', { name: /cambiar a tema claro/i }));

    // Verificar
    expect(document.documentElement).not.toHaveClass('dark');
    expect(localStorage.getItem('rh.tema')).toBe('claro');

    // Actuar: claro → oscuro
    await usuario.click(screen.getByRole('button', { name: /cambiar a tema oscuro/i }));

    // Verificar
    expect(document.documentElement).toHaveClass('dark');
    expect(localStorage.getItem('rh.tema')).toBe('oscuro');
  });

  it('ignora un valor guardado que no es una preferencia válida', () => {
    // Preparar
    simularSistema(false);
    localStorage.setItem('rh.tema', 'violeta');

    // Actuar
    montar();

    // Verificar
    expect(screen.getByRole('button', { name: /tema del sistema/i })).toBeInTheDocument();
    expect(document.documentElement).not.toHaveClass('dark');
  });
});
