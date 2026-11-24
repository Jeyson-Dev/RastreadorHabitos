import { createContext, useContext } from 'react';

export const ROL_ADMINISTRADOR = 'Administrador';
export const CLAVE_USUARIO = ['sesion', 'usuario'];

export const ContextoSesion = createContext(null);

export function useSesion() {
  const contexto = useContext(ContextoSesion);
  if (!contexto) throw new Error('useSesion debe usarse dentro de ProveedorSesion.');
  return contexto;
}
