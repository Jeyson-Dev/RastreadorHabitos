import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CLAVES_HABITOS, consultarRegistros, crearHabito, listarHabitos, registrarCumplimiento } from '../../api/habitos';

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
