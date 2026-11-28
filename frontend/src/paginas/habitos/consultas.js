import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CLAVES_METAS } from '../../api/metas';
import {
  archivarHabito,
  CLAVES_HABITOS,
  consultarRegistros,
  crearHabito,
  editarHabito,
  eliminarHabito,
  listarHabitos,
  registrarCumplimiento,
  restaurarHabito,
} from '../../api/habitos';

export function useHabitos() {
  return useQuery({ queryKey: CLAVES_HABITOS.lista, queryFn: ({ signal }) => listarHabitos(signal) });
}

export function useRegistros(id, { desde, hasta } = {}) {
  return useQuery({
    queryKey: CLAVES_HABITOS.registros(id, desde, hasta),
    queryFn: ({ signal }) => consultarRegistros(id, { desde, hasta }, signal),
    enabled: Boolean(id),
    placeholderData: keepPreviousData,
  });
}

// Tras cualquier cambio se recargan los hábitos y sus historiales, pero no el plan de hoy: el clima
// no cambia por registrar un hábito y cada consulta llama a un servicio externo.
function invalidarHabitos(clienteConsultas) {
  return clienteConsultas.invalidateQueries({
    predicate: ({ queryKey }) => queryKey[0] === 'habitos' && queryKey[1] !== 'plan-de-hoy',
  });
}

export function useRegistrarCumplimiento() {
  const clienteConsultas = useQueryClient();
  return useMutation({
    mutationFn: ({ id, cumplido }) => registrarCumplimiento(id, cumplido),
    onSuccess: () => invalidarHabitos(clienteConsultas),
  });
}

export function useCrearHabito() {
  const clienteConsultas = useQueryClient();
  return useMutation({
    mutationFn: crearHabito,
    onSuccess: () => invalidarHabitos(clienteConsultas),
  });
}

// Editar, archivar y restaurar cambian el hábito y sus rachas; eliminar además cambia el avance de
// las metas que lo incluían, por eso recarga también las metas (y el plan de hoy, que lo listaba).
function useMutacionHabito(mutationFn, { tocaMetas = false } = {}) {
  const clienteConsultas = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await invalidarHabitos(clienteConsultas);
      await clienteConsultas.invalidateQueries({ queryKey: CLAVES_HABITOS.planDeHoy(undefined).slice(0, 2) });
      if (tocaMetas) await clienteConsultas.invalidateQueries({ queryKey: CLAVES_METAS.lista });
    },
  });
}

export const useEditarHabito = () => useMutacionHabito(({ id, ...datos }) => editarHabito(id, datos));
export const useArchivarHabito = () => useMutacionHabito((id) => archivarHabito(id));
export const useRestaurarHabito = () => useMutacionHabito((id) => restaurarHabito(id));
export const useEliminarHabito = () => useMutacionHabito((id) => eliminarHabito(id), { tocaMetas: true });
