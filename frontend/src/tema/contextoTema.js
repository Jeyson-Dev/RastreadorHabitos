import { createContext, useContext } from 'react';

// Preferencias posibles: seguir al sistema, o fijar claro u oscuro.
export const PREFERENCIAS_TEMA = ['sistema', 'claro', 'oscuro'];
export const CLAVE_TEMA = 'rh.tema';

export const ContextoTema = createContext(null);

export function useTema() {
  const contexto = useContext(ContextoTema);
  if (!contexto) throw new Error('useTema debe usarse dentro de ProveedorTema.');
  return contexto;
}
