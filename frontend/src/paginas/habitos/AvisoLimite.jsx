import { useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { mensajeDeError } from '../../api/cliente';
import { CLAVES_SOLICITUDES, crearSolicitudPermiso, listarSolicitudesPendientes, PERMISO_LIMITE_AMPLIADO } from '../../api/solicitudes';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import { CLAVE_USUARIO } from '../../sesion/contextoSesion';

// Aviso del límite de hábitos (10, o 20 con LIMITE_HABITOS_AMPLIADO) con la solicitud del permiso.
// Gestión de permisos no impide pedirlo dos veces, así que el botón se oculta si ya hay una
// solicitud pendiente. Al montarse vuelve a pedir el usuario, por si el permiso ya se aprobó.
export default function AvisoLimite({ cantidad, limite, ampliado, alLlegarAlLimite = false }) {
  const clienteConsultas = useQueryClient();
  const restantes = Math.max(limite - cantidad, 0);
  const mostrar = alLlegarAlLimite || restantes <= 2;

  useEffect(() => {
    clienteConsultas.invalidateQueries({ queryKey: CLAVE_USUARIO });
  }, [clienteConsultas]);

  const pendientes = useQuery({
    queryKey: CLAVES_SOLICITUDES.pendientes,
    queryFn: ({ signal }) => listarSolicitudesPendientes(signal),
    enabled: mostrar && !ampliado,
  });
  const solicitar = useMutation({
    mutationFn: () => crearSolicitudPermiso(PERMISO_LIMITE_AMPLIADO),
    onSuccess: () => clienteConsultas.invalidateQueries({ queryKey: CLAVES_SOLICITUDES.pendientes }),
  });

  if (!mostrar) return null;

  if (ampliado) {
    return restantes === 0 ? (
      <Aviso tipo="aviso" titulo="Llegaste al máximo de 20 hábitos">
        Para crear uno nuevo, tendrías que dejar de usar otro.
      </Aviso>
    ) : null;
  }

  const yaPedido = pendientes.data?.some((s) => s.permiso === PERMISO_LIMITE_AMPLIADO) || solicitar.isSuccess;
  const titulo = restantes === 0 ? `Llegaste al límite de ${limite} hábitos` : `Te ${restantes === 1 ? 'queda 1 hábito' : `quedan ${restantes} hábitos`}`;

  return (
    <Aviso tipo={restantes === 0 ? 'aviso' : 'info'} titulo={titulo}>
      {yaPedido ? (
        <p>Tu solicitud del límite de 20 hábitos está pendiente. Te avisaremos cuando un administrador la resuelva.</p>
      ) : (
        <div className="flex flex-col items-start gap-2">
          <p>¿Necesitas más? Pide el permiso para tener hasta 20 hábitos; lo aprueba un administrador.</p>
          <Boton tamano="sm" variante="secundario" cargando={solicitar.isPending} disabled={pendientes.isPending} onClick={() => solicitar.mutate()}>
            Pedir límite de 20
          </Boton>
          {solicitar.isError && <p className="text-xs font-medium text-peligro">{mensajeDeError(solicitar.error)}</p>}
        </div>
      )}
    </Aviso>
  );
}
