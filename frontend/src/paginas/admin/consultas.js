import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { CLAVES_ADMIN, listarUsuarios } from '../../api/admin';

export function useUsuarios() {
  return useQuery({ queryKey: CLAVES_ADMIN.usuarios, queryFn: ({ signal }) => listarUsuarios(signal) });
}

// Id → «Nombre (correo)» para mostrar personas donde la API solo entrega el Id.
export function useNombresUsuarios() {
  const usuarios = useUsuarios();
  return useMemo(() => {
    const mapa = new Map((usuarios.data ?? []).map((u) => [u.id, u]));
    return (id) => {
      const u = mapa.get(id);
      return u ? `${u.nombreCompleto} (${u.email})` : id ? `Usuario ${String(id).slice(0, 8)}…` : 'Sistema';
    };
  }, [usuarios.data]);
}
