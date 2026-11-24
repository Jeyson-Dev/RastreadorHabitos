// Configuración común de Vitest: matchers de Testing Library y limpieza entre pruebas.
import '@testing-library/jest-dom/vitest';
import { afterEach } from 'vitest';
import { cleanup } from '@testing-library/react';

afterEach(() => {
  cleanup();
  localStorage.clear();
});

// jsdom no implementa matchMedia; las pruebas del tema lo reemplazan cuando lo necesitan.
if (!window.matchMedia) {
  window.matchMedia = (consulta) => ({
    matches: false,
    media: consulta,
    addEventListener: () => {},
    removeEventListener: () => {},
  });
}
