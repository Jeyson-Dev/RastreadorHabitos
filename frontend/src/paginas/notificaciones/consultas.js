import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  actualizarPreferencias,
  CLAVES_NOTIFICACIONES,
  listarNotificaciones,
  marcarLeida,
  obtenerPreferencias,
} from '../../api/notificaciones';
import { useSesion } from '../../sesion/contextoSesion';

const UN_MINUTO = 60 * 1000;

export function useBandeja(soloNoLeidas) {
  return useQuery({
    queryKey: CLAVES_NOTIFICACIONES.bandeja(soloNoLeidas),
    queryFn: ({ signal }) => listarNotificaciones(soloNoLeidas, signal),
  });
}

// Contador de no leídas para la campana: se actualiza cada minuto mientras la pestaña está visible.
export function useNoLeidas() {
  const { autenticado } = useSesion();
  const consulta = useQuery({
    queryKey: CLAVES_NOTIFICACIONES.noLeidas,
    queryFn: ({ signal }) => listarNotificaciones(true, signal),
    enabled: autenticado,
    refetchInterval: UN_MINUTO,
    retry: false,
  });
  return consulta.data?.noLeidas ?? 0;
}

export function useMarcarLeida() {
  const clienteConsultas = useQueryClient();
  return useMutation({
    mutationFn: marcarLeida,
    onSuccess: () => clienteConsultas.invalidateQueries({ queryKey: ['notificaciones', 'bandeja'] }),
  });
}

export function usePreferencias() {
  return useQuery({ queryKey: CLAVES_NOTIFICACIONES.preferencias, queryFn: ({ signal }) => obtenerPreferencias(signal) });
}

export function useActualizarPreferencias() {
  const clienteConsultas = useQueryClient();
  return useMutation({
    mutationFn: actualizarPreferencias,
    onSuccess: (datos) => clienteConsultas.setQueryData(CLAVES_NOTIFICACIONES.preferencias, datos),
  });
}
