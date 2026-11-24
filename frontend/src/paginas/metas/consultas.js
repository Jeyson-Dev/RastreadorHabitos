import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CLAVES_METAS, completarMeta, crearMeta, listarMetas, reactivarMeta } from '../../api/metas';

export function useMetas() {
  return useQuery({ queryKey: CLAVES_METAS.lista, queryFn: ({ signal }) => listarMetas(signal) });
}

// Cada cambio recarga la lista: el servidor recalcula avance y estado al consultar.
function useMutacionMeta(mutationFn) {
  const clienteConsultas = useQueryClient();
  return useMutation({ mutationFn, onSuccess: () => clienteConsultas.invalidateQueries({ queryKey: CLAVES_METAS.lista }) });
}

export const useCrearMeta = () => useMutacionMeta(crearMeta);
export const useCompletarMeta = () => useMutacionMeta((id) => completarMeta(id));
export const useReactivarMeta = () => useMutacionMeta(({ id, nuevaFechaLimite }) => reactivarMeta(id, nuevaFechaLimite));
